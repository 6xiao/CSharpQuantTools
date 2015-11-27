using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace FixEngine
{
    class CommandProcess
    {
        protected const string CmdRequestUserLogin = "RequestUserLogin";
        protected const string CmdRequestUserLogout = "RequestUserLogout";
        protected const string CmdLimitOrder = "SendLimitOrder";
        protected const string CmdMarketOrder = "SendMarketOrder";
        protected const string CmdCancelLimitOrder = "CancelLimitOrder";
        protected const string CmdOpenPositions = "RequestOpenPositions";
        protected const string CmdTotalEquity = "RequestTotalEquity";
        protected const string CmdDeal = "RequestDeal";
        protected const string CmdPostfix = "_Notification";

        protected const string Src = "Source";
        protected const string Dest = "Destination";
        protected const string MID = "MessageId";
        protected const string Symbol = "TickerName";
        protected const string Price = "Price";
        protected const string Qty = "Quantity";
        protected const string InternalOrderID = "InternalOrderId";
        protected const string MsgFmt = "Source={0},Destination={1},MessageId={2}{{{{{3}{4}{{0}}}}}}";

        //KV(ClientOrderID, MsgHeaderFormat) as (01020304, Source=FIX,Destination=OE,MessageId=33{{0}})
        private Dictionary<string, string> dictID = new Dictionary<string, string>(); 
        //KV(ClientOrderID, OrderQuantity) as (01020304,4) / (01020304,-3)
        private Dictionary<string, int> orderQty = new Dictionary<string, int>();

        protected FixCallBack ReportCallBack { get; set; }
        internal FixApplication App { get; set; }

        internal CommandProcess(FixCallBack report, FixApplication fixapp)
        {
            App = fixapp;

            this.ReportCallBack = report;
            App.ReportCallBack = this.CmdReport;
        }

        private void CmdCallBack(string RspMsg)
        {
            if (ReportCallBack != null)
            {
                ReportCallBack(RspMsg);
            }
        }

        private void CmdReport(int type, string id, string connect)
        {
            try
            {
                //特殊消息处理
                switch (type)
                {
                    case FixApplication.msgTypeLogin:
                        CmdCallBack(
                            string.Format(
                            string.Format(MsgFmt, "Fix", "OE", "-1", CmdRequestUserLogin, CmdPostfix), 
                            string.Empty));
                        return;

                    case FixApplication.msgTypeLogout:
                        CmdCallBack(
                            string.Format(
                            string.Format(MsgFmt, "Fix", "OE", "-1", CmdRequestUserLogout, CmdPostfix), 
                            string.Empty));
                        return;
                }

                //普通异步消息，将保存在内存的消息头与消息体结合回报即可
                CmdCallBack(string.Format(dictID[id], connect));
            }
            catch (Exception ex)
            {
                //如果内存中没有对应消息体的消息头，则只回报消息体
                CmdCallBack(connect);
                Fix.Out(ex.ToString());
            }
        }

        //返回值参考 ： FixExecutor.DealCommand
        internal int DealCommand(string msg)
        {
            try
            {
                var mb = GetMsgBody(msg);
                var cid_OR_result = string.Empty;
                var messageFormat = string.Empty;
                var complete = false;

                if (mb.Contains(CmdLimitOrder))
                {
                    messageFormat = MsgFormat(msg, CmdLimitOrder);
                    cid_OR_result = ProcLimitOrder(ref complete, mb);
                }
                else if (mb.Contains(CmdMarketOrder))
                {
                    messageFormat = MsgFormat(msg, CmdMarketOrder);
                    cid_OR_result = ProcMarketOrder(ref complete, mb);
                }
                else if (mb.Contains(CmdCancelLimitOrder))
                {
                    messageFormat = MsgFormat(msg, CmdCancelLimitOrder);
                    cid_OR_result = ProcCancelOrder(ref complete, mb);
                }
                else if (mb.Contains(CmdDeal))
                {
                    messageFormat = MsgFormat(msg, CmdDeal);
                    cid_OR_result = ProcRequestDeal(ref complete);
                }
                else if (mb.Contains(CmdOpenPositions))
                {
                    messageFormat = MsgFormat(msg, CmdOpenPositions);
                    cid_OR_result = ProcRequestOpenPositions(ref complete, mb);
                }
                else if (mb.Contains(CmdTotalEquity))
                {
                    messageFormat = MsgFormat(msg, CmdTotalEquity);
                    cid_OR_result = ProcRequestTotalEquity(ref complete);
                }
                else 
                {
                    CmdCallBack("Command UnDefined : " + msg);
                    return 0;
                }

                //complete = true 时： Proc函数返回的是结果
                //complete = false 时： Proc函数返回的是ClientOrderID
                //Proc函数返回空字符串，表示执行失败

                if (string.IsNullOrEmpty(cid_OR_result))
                {
                    CmdCallBack("Command Process Error/Unsupport : " + msg);
                    return 0;
                }

                if (complete)
                {
                    //同步执行完成的命令
                    CmdCallBack(string.Format(messageFormat, cid_OR_result));
                    return 2;
                }
                
                //等待异步消息的命令，将消息头保存在内存
                lock (dictID)
                {
                    dictID.Add(cid_OR_result, messageFormat);
                }

                return 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return -1;
            }
        }
       
        private string ProcLimitOrder(ref bool comp, string msg)
        {
            try
            {
                var symbol = GetField(msg, Symbol);
                var price = GetField(msg, Price);
                var qty = int.Parse(GetField(msg, Qty));
                var tmp = double.Parse(price);

                if (string.IsNullOrEmpty(symbol) || qty == 0)
                {
                    return string.Empty;
                }
 
                var cid = App.ProcLimitOrder(ref comp, symbol, price, qty);

                if (!string.IsNullOrEmpty(cid))
                {
                    lock (orderQty)
                    {
                        orderQty.Add(cid, qty);
                    }

                    App.AddUnFilledOrder("LimitOrder", symbol, qty, price, cid);
                }

                return cid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return string.Empty;
            }
        }

        private string ProcMarketOrder(ref bool comp, string msg)
        {
            try
            {
                var symbol = GetField(msg, Symbol);
                var price = GetField(msg, Price);
                var qty = int.Parse(GetField(msg, Qty));
                var tmp = double.Parse(price);

                if (string.IsNullOrEmpty(symbol) || qty == 0)
                {
                    return string.Empty;
                }
                
                var cid = App.ProcMarketOrder(ref comp, symbol, price, qty);

                if (!string.IsNullOrEmpty(cid))
                {
                    lock (orderQty)
                    {
                        orderQty.Add(cid, qty);
                    }

                    App.AddUnFilledOrder("MarketOrder", symbol, qty, price, cid);
                }

                return cid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return string.Empty;
            }
        }

        private string ProcCancelOrder(ref bool comp, string msg)
        {
            try
            {
                var id = GetField(msg, InternalOrderID);
                var symbol = GetField(msg, Symbol);

                if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(id))
                {
                    return string.Empty;
                }

                return App.ProcCancelOrder(ref comp, symbol, id, orderQty[id]);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return string.Empty;
        }

        private string ProcRequestOpenPositions(ref bool comp, string msg)
        {
            var symbol = GetField(msg, Symbol);
            return App.ProcRequestOpenPositions(ref comp, symbol);
        }

        private string ProcRequestTotalEquity(ref bool comp)
        {
            return App.ProcRequestTotalEquity(ref comp);
        }

        private string ProcRequestDeal(ref bool comp)
        {
            return App.ProcRequestDeal(ref comp);
        }

        private static string MsgFormat(string msg, string cmd)
        {
            var dest = GetField(msg, Src);
            var src = GetField(msg, Dest);
            var mid = GetField(msg, MID);

            return string.Format(MsgFmt, src, dest, mid, cmd, CmdPostfix);
        }

        private static string GetMsgBody(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return string.Empty;
            }

            int s = msg.IndexOf("{");
            if (s < 0)
            {
                return string.Empty;
            }
            msg = msg.Remove(0, s + 1);

            int e = msg.IndexOf("}");
            if (e < 0)
            {
                return string.Empty;
            }

            return msg.Remove(e);
        }

        private static string GetField(string msg, string key)
        {
            if (string.IsNullOrEmpty(msg) || string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var spa = msg.Split(new char[] { ',','{', '}' });
            foreach (var s in spa)
            {
                if (s.StartsWith(key + "="))
                {
                    return s.Remove(0, key.Length + 1);
                }
            }

            return string.Empty;
        }
    }// class CommandProcess
}
