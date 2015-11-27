using System;
using System.Collections.Generic;
using System.Diagnostics;
using QuickFix;

namespace FixEngine
{
    public class FixApplication : QuickFix.MessageCracker, QuickFix.Application
    {
        internal const int msgTypeError = -1;
        internal const int msgTypeNone = 0;
        internal const int msgTypeAdmin = 1;
        internal const int msgTypeCancel = 2;
        internal const int msgTypeLogin = 3;
        internal const int msgTypeLogout = 4;
        internal const int msgTypeReject = 5;
        internal const int msgTypeBsnsRej = 6;
        internal const int msgTypeQueued = 7;
        internal const int msgTypePartFill = 8;
        internal const int msgTypeFullFill = 9;

        internal FixReportMessage ReportCallBack { get; set; }

        protected const string rspOrderStatus = ",Status={0},TickerName={1},FilledPrice={2},FilledQuantity={3},AckInternalOrderId={4}";
        protected const string rspError = ",Error={0}";
        protected const string rspCancel = ",CanceledOrderId={0}";
        protected const string DealEqu = ",Deal=";
        protected const string OpenPositionsEqu = ",OpenPositions=";
        protected const string TotalEquityEqu = ",TotalEquity=";
        protected const string Reject = "Rejected";
        protected const string Queued = "Queued";
        protected const string FullFill = "Filled";
        protected const string PartFill = "PartiallyFilled";

        protected const int ClientOrderIdIndex = 11;
        protected const int CancelClientOrderIdIndex = 41;
        protected const int SymbolIndex = 55;
        protected const int TextIndex = 58;
        protected const int PriceIndex = 31;
        protected const int QtyIndex = 32;
        protected const int ReportIndex = 39;
        protected const int SideIndex = 54;
        protected const char SymbolSplitChar = '@';
        protected const string SideBuy = "1";
        protected const string SideSell = "2";
        protected const string MarketOrder = "1";
        protected const string LimitOrder = "2";

        private object lockObj = new object();
        private volatile int lastClOrdID = (int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds; //最后的ClientOrderID，同一天内不可重复。
        private readonly List<SessionID> sessionList = new List<SessionID>(); //会话集合，总是使用第一个（一般只有一个会话）
        private DealList dealList = new DealList(); // 成交列表，保存在内存中，可以查成交记录和某合约的持仓情况
        private UnFillOrders unFill = new UnFillOrders(); // 未成交列表，保存在内存中，可以实时反映下单情况

        //KV(ClientOrderID, TickerName's Value) as  (01020304, Futures@CSI300@201112@cffe)
        private Dictionary<string, string> symbolList = new Dictionary<string, string>(); //TickerName列表

        //命令处理函数集Proc**(ref bool comp, ...);
        //comp 表示完成情况，同步操作完成（比如从内存中查成效记录）则置为true.否则保持为false（等待异步消息）
        //comp = true 时： Proc函数返回的是结果
        //comp = false 时： Proc函数返回的是ClientOrderID
        //返回空字符串，表示执行失败
        public virtual string ProcLimitOrder(ref bool comp, string symbol, string price, int qty)
        { return string.Empty; }
        public virtual string ProcMarketOrder(ref bool comp, string symbol, string price, int qty)
        { return string.Empty; }
        public virtual string ProcCancelOrder(ref bool comp, string symbol, string id, int qty)
        { return string.Empty; }

        public virtual string ProcRequestOpenPositions(ref bool comp, string symbol)
        {
            comp = true;
            unFill.GetUnFillOrders();
            return OpenPositionsEqu + dealList.GetOpenPositionList(symbol);
        }

        public virtual string ProcRequestTotalEquity(ref bool comp)
        {
            comp = true;
            return TotalEquityEqu + "0";
        }

        public virtual string ProcRequestDeal(ref bool comp)
        {
            comp = true;
            return DealEqu + dealList.GetDealList();
        }

        public void AddUnFilledOrder(string status, string symbol, int qty, string price, string clientID)
        {
            unFill.Add(status, symbol, qty, price, clientID);
        }

        protected virtual bool SplitSymbol(string symbol, ref string fut, ref string contract, ref string maturity, ref string exchange)
        {
            var spa = symbol.Split(new[] { SymbolSplitChar });

            try
            {
                fut = spa[0];
                contract = spa[1];
                maturity = spa[2];
                exchange = spa[3];
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        //成交处理函数，默认是把成交结果保存到内存
        protected virtual void DefDealProc(string symbol, string price, string qty, string clientID)
        {
            if (string.IsNullOrEmpty(symbol)
                || string.IsNullOrEmpty(price)
                || string.IsNullOrEmpty(qty)
                || string.IsNullOrEmpty(clientID)
                || !dealList.Add(symbol, qty, price, clientID))
            {
                FixReport(msgTypeError, null, 0, "Invalidate Opertion : Check the follow message!");
            }
        }
        
        //返回某个合约的持仓
        protected int GetOpenPosition(string symbol)
        {
            return dealList.GetOpenPosition(symbol);
        }

        protected virtual void MsgHeartbeat(Message msg, SessionID sid){}

        //网关拒绝撤单
        protected virtual void MsgOrderCancelReject(Message msg, SessionID sid)
        {
            try
            {
                FixReport(msgTypeCancel, msg, CancelClientOrderIdIndex, 
                    string.Format(rspCancel, msg.getField(CancelClientOrderIdIndex))
                    + string.Format(rspError, msg.getField(TextIndex)));
            }
            catch (Exception){}
        }

        //网关拒绝处理消息
        protected virtual void MsgReject(Message msg, SessionID sid)
        {
            FixReport(msgTypeReject, msg, 0, msg.ToString());
        }

        protected virtual void MsgBusinessReject(Message msg, SessionID sid)
        {
            FixReport(msgTypeBsnsRej, msg, 0, msg.ToString());
        }

        //消息执行结果
        protected virtual void MsgExecutionReport(Message msg, SessionID sid)
        {
            ReportExcuResult(GetSymbol(msg), msg, sid);
        }

        //生成返回消息体
        protected virtual void ReportExcuResult(string symbol, Message msg, SessionID sid)
        {
            try
            {
                var cs = msg.getField(ReportIndex);
                var oid = msg.getField(ClientOrderIdIndex);

                if (cs == "0") //queued
                {
                    FixReport(msgTypeQueued, msg, ClientOrderIdIndex, string.Format(rspOrderStatus, Queued, symbol, 0, 0, oid));
                    unFill.ChangeStatus(oid, "Queued");
                }
                else if (cs == "8") // reject
                {
                    var res = string.Format(rspOrderStatus, Reject, symbol, 0, 0, oid);
                    res += string.Format(rspError, msg.getField(TextIndex));
                    FixReport(msgTypeReject, msg, ClientOrderIdIndex, res);
                    unFill.ChangeStatus(oid, "Reject");
                    unFill.Remove(oid);
                }
                else if (cs == "4") // cancel
                {
                    //目前4消息分两种情况：撤单消息返回、市价单不成功或不完全成功。
                    //撤单消息返回时：有41,44字段。
                    //市价单不成功时：无41，44字段。
                    if (msg.isSetField(41) && msg.isSetField(44))
                    {
                        FixReport(msgTypeCancel, msg, ClientOrderIdIndex, string.Format(rspCancel, msg.getField(41)));
                    }
                    else
                    {
                        var res = string.Format(rspOrderStatus, Reject, symbol, 0, 0, oid);
                        res += string.Format(rspError, "MarketOrder is IOC/FOK(TimeInForce)");
                        FixReport(msgTypeReject, msg, ClientOrderIdIndex, res);
                    }
                    unFill.ChangeStatus(oid, "Canceled Or MarketOrderCanceled");
                    unFill.Remove(oid);
                }
                else if (cs == "1") //partiallyFilled
                {
                    var price = msg.getField(PriceIndex);
                    var qty = (msg.getField(SideIndex) == SideSell ? "-" : "") + msg.getField(QtyIndex);

                    DefDealProc(symbol, price, qty, oid);
                    FixReport(msgTypePartFill, msg, ClientOrderIdIndex, 
                        string.Format(rspOrderStatus, PartFill, symbol, price, qty, oid));
                    unFill.ChangeStatus(oid, "PartiallyFilled");
                }
                else if (cs == "2") //filled
                {
                    var price = msg.getField(PriceIndex);
                    var qty = (msg.getField(SideIndex) == SideSell ? "-" : "") + msg.getField(QtyIndex);

                    DefDealProc(symbol, price, qty, oid);
                    FixReport(msgTypePartFill, msg, ClientOrderIdIndex, 
                        string.Format(rspOrderStatus, FullFill, symbol, price, qty, oid));
                    unFill.ChangeStatus(oid, "Filled");
                    unFill.Remove(oid);
                }
            }
            catch (Exception) { }
        }

        protected void FixReport(int type, Message msg, int IDfieldNum, string connect)
        {
            if (ReportCallBack != null)
            {
                try
                {
                    ReportCallBack(type, msg.getField(IDfieldNum), connect);
                }
                catch (Exception)
                {
                    ReportCallBack(type, string.Empty, connect);
                }
            }
        }

        protected void SaveSymbol(string cid, string symbol)
        {
            lock (symbolList)
            {
                if (!symbolList.ContainsKey(cid))
                {
                    symbolList.Add(cid, symbol);
                }
            }
        }

        protected string GetSymbol(Message msg)
        {
            try
            {
                return symbolList[msg.getField(ClientOrderIdIndex)];
            }
            catch (Exception){}

            try
            {
                return msg.getField(SymbolIndex);
            }
            catch (Exception){}

            return string.Empty;
        }

        protected virtual string AllocClOrdID(string symbol)
        {
            lock (lockObj)
            {
                var id = ++lastClOrdID;
                var str  = id.ToString();
                SaveSymbol(str, symbol);
                Fix.Out("Last Client Order ID : " + str);
                return str;
            }
        }

        protected SessionID TradingSession()
        {
            return sessionList.Count > 0 ? sessionList[0] : null;
        }

        protected bool SendMessageToSession(QuickFix.Message msg)
        {
            return sessionList.Count > 0 && msg != null && Session.sendToTarget(msg, sessionList[0]);
        }

        protected string GetTimeString()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd-hh:mm:ss");
        }

        protected bool IsBuy(int qty)
        {
            return qty > 0;
        }

        protected bool IsOpen(int pos, int qty)
        {
            return pos == 0 || pos * qty > 0;
        }

        protected bool IsClose(int pos, int qty)
        {
            return pos * qty < 0 && pos + qty == 0;
        }

        protected bool IsTransSide(int pos, int qty)
        {
            return pos * qty < 0 && pos + qty != 0;
        }

        public void onCreate(SessionID sessionID)
        {
        }

        //登录成功
        public void onLogon(SessionID sessionID)
        {
            Fix.Out("Logon");
            sessionList.Remove(sessionID);
            sessionList.Add(sessionID);

            if (sessionList.Count == 1)
            {
                FixReport(msgTypeLogin, null, 0, string.Empty);
            }
        }

        //退出或登录失败（如果是没有登录失败，会不断尝试登录）
        public void onLogout(SessionID sessionID)
        {
            sessionList.Remove(sessionID);

            if (sessionList.Count == 0)
            {
                FixReport(msgTypeLogout, null, 0, string.Empty);
            }
        }

        //发送Admin消息到网关（比如心跳，登录，登出，序列号相关的消息等）
        public void toAdmin(QuickFix.Message message, SessionID sessionID)
        {
            Fix.Out(message);
        }
        
        //发送应用消息到网关（比如下单，撤单，查询相关消息等）
        public void toApp(QuickFix.Message message, SessionID sessionID)
        {
            Fix.Out(message);
        }

        //从网关返回的Admin消息
        public void fromAdmin(QuickFix.Message message, SessionID sessionID)
        {
            crack(message, sessionID);
            Fix.Out(message);
        }

        //从网关返回的应用消息
        public void fromApp(QuickFix.Message message, SessionID sessionID)
        {
            crack(message, sessionID);
            Fix.Out(message);
        }

        public sealed override void onMessage(QuickFix42.Advertisement message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.Allocation message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.AllocationACK message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.BidRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.BidResponse message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.BusinessMessageReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgBusinessReject(message, session);
        }

        public sealed override void onMessage(QuickFix42.DontKnowTrade message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.Email message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ExecutionReport message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgExecutionReport(message, session);
        }
        
        public sealed override void onMessage(QuickFix42.Heartbeat message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgHeartbeat(message, session);
        }

        public sealed override void onMessage(QuickFix42.IndicationofInterest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ListCancelRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ListExecute message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ListStatus message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ListStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ListStrikePrice message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.MarketDataIncrementalRefresh message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.MarketDataRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.MarketDataRequestReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.MarketDataSnapshotFullRefresh message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.MassQuote message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.NewOrderList message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.NewOrderSingle message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.News message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.OrderCancelReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgOrderCancelReject(message, session);
        }

        public sealed override void onMessage(QuickFix42.OrderCancelReplaceRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.OrderCancelRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.OrderStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.Quote message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.QuoteAcknowledgement message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.QuoteCancel message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.ResendRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.QuoteRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.QuoteStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.Reject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgReject(message, session);
        }

        public sealed override void onMessage(QuickFix42.SecurityDefinition message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.SecurityDefinitionRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.SecurityStatus message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.SecurityStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.SequenceReset message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.SettlementInstructions message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.TestRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.TradingSessionStatus message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix42.TradingSessionStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.Advertisement message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.Allocation message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString()); 
        }

        public sealed override void onMessage(QuickFix43.AllocationAck message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.BidRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString()); 
        }

        public sealed override void onMessage(QuickFix43.BidResponse message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.BusinessMessageReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgBusinessReject(message, session);
        }

        public sealed override void onMessage(QuickFix43.CrossOrderCancelReplaceRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.CrossOrderCancelRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.DerivativeSecurityList message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.DerivativeSecurityListRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.DontKnowTrade message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.Email message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.ExecutionReport message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgExecutionReport(message, session);
        }

        public sealed override void onMessage(QuickFix43.Heartbeat message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgHeartbeat(message, session);
        }

        public sealed override void onMessage(QuickFix43.IOI message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.ListCancelRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.ListExecute message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.ListStatus message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.ListStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.ListStrikePrice message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MarketDataIncrementalRefresh message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MarketDataRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MarketDataRequestReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MarketDataSnapshotFullRefresh message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MassQuote message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MassQuoteAcknowledgement message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.NewOrderCross message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.MultilegOrderCancelReplaceRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.NewOrderList message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.NewOrderMultileg message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.NewOrderSingle message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.News message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.OrderCancelReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgOrderCancelReject(message, session);
        }

        public sealed override void onMessage(QuickFix43.OrderCancelReplaceRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.OrderCancelRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.OrderMassCancelReport message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.OrderMassCancelRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.OrderMassStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.OrderStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.Quote message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.QuoteCancel message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.QuoteRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.QuoteRequestReject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.QuoteStatusReport message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.QuoteStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.RegistrationInstructions message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.RegistrationInstructionsResponse message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.Reject message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
            MsgReject(message, session);
        }

        public sealed override void onMessage(QuickFix43.ResendRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.RFQRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityDefinition message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityDefinitionRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityList message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityListRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityStatus message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityTypeRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SecurityTypes message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SequenceReset message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.SettlementInstructions message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.TestRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.TradingSessionStatusRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.TradeCaptureReport message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.TradeCaptureReportRequest message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }

        public sealed override void onMessage(QuickFix43.TradingSessionStatus message, SessionID session)
        {
            Fix.Out(new StackTrace(new StackFrame(true)).GetFrame(0).GetMethod().ToString());
        }
    }//end of class FixApplication
}
