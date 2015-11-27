using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;

namespace FixEngine
{
    internal class DealList
    {
        private class PQC
        {
            internal string tm = DateTime.Now.ToString("yyyyMMdd");
            internal int Qty { get; set; }
            internal string Price { get; set; }
            internal string ClientID { get; set; }

            internal PQC(int q, string p, string c)
            {
                Qty = q;
                Price = p;
                ClientID = c;
            }
        }

        Dictionary<string, List<PQC>> deals = new Dictionary<string,List<PQC>>();

        //添加一条成交记录
        internal bool Add(string symbol, string qty, string price, string clientID)
        {
            try
            {
                return Add(symbol, int.Parse(qty), price, clientID);
            }
            catch (Exception)
            {
                return false;
            }
        }

        //同上
        internal bool Add(string symbol, int qty, string price, string clientID)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(price)
                || string.IsNullOrEmpty(clientID) || qty == 0)
            {
                return false;
            }

            try
            {
                lock (deals)
                {
                    if (!deals.ContainsKey(symbol))
                    {
                        deals.Add(symbol, new List<PQC>());
                    }

                    deals[symbol].Add(new PQC(qty, price, clientID));
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        //返回某合约的持仓
        internal int GetOpenPosition(string symbol)
        {
            try
            {
                int q = 0;
                var tm = DateTime.Now.ToString("yyyyMMdd");
                foreach (var pqc in deals[symbol])
                {
                    if (tm == pqc.tm)
                    {
                        q += pqc.Qty;
                    }
                }

                return q;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        //返回持仓信息
        internal string GetOpenPositionList(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                var rs = string.Empty;

                foreach (var key in deals.Keys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        var op = GetOpenPosition(key);
                        if (op == 0) continue;
                        
                        if (string.IsNullOrEmpty(rs))
                        {
                            rs = string.Format("{0}={1}", key, op);
                        }
                        else
                        {
                            rs = rs + string.Format("|{0}={1}", key, op);
                        }
                    }
                }

                return rs;
            }
            else
            {
                var op = GetOpenPosition(symbol);
                return op == 0 ? string.Empty : string.Format("{0}={1}", symbol, op);
            }
        }

        //返回成交列表
        internal string GetDealList()
        {
            try
            {
                var rs = string.Empty;
                var tm = DateTime.Now.ToString("yyyyMMdd");
                foreach (var deal in deals)
                {
                    foreach (var pq in deal.Value)
                    {
                        if (pq.tm == tm)
                        {
                            rs += string.Format("{0}={1}*{2}|", deal.Key, pq.Price, pq.Qty);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(rs))
                {
                    rs = rs.Remove(rs.Length - 1);
                }

                return rs;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    internal class UnFillOrders
    {
        internal class UFO
        {
            internal int Qty { get; set; }
            internal string Price { get; set; }
            internal string Symbol { get; set; }
            internal string Status { get; set; }

            internal UFO(string s, int q, string p, string status)
            {
                Symbol = s;
                Qty = q;
                Price = p;
                Status = status;
            }
        }

        Dictionary<string, UFO> ufos = new Dictionary<string, UFO>();

        //添加一条成交记录
        internal bool Add(string status, string symbol, string qty, string price, string clientID)
        {
            try
            {
                return Add(status, symbol, int.Parse(qty), price, clientID);
            }
            catch (Exception)
            {
                return false;
            }
        }

        //同上
        internal bool Add(string status, string symbol, int qty, string price, string clientID)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(price)
                || string.IsNullOrEmpty(clientID) || qty == 0)
            {
                return false;
            }

            try
            {
                if (!ufos.ContainsKey(clientID))
                {
                    ufos.Add(clientID, new UFO(symbol, qty, price, status));
                }

                var s = string.Format("SendOrder # ClientOrderID: {0} # T:{1} # S:{2} # P:{3} # Q:{4}",
                        clientID, symbol, status, price, qty);
                Fix.Out(s);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void Remove(string clientID)
        {
            try
            {
                ufos.Remove(clientID);
                Fix.Out(string.Format("RemoveOrder # ClientOrderID: {0}", clientID));
            }
            catch (Exception ex)
            {
                Fix.Out(ex.ToString());
            }
        }

        internal void ChangeStatus(string clientID, string status)
        {
            if (ufos.ContainsKey(clientID))
            {
                ufos[clientID].Status = status;
            }
        }

        //返回成交列表
        internal void GetUnFillOrders()
        {
            try
            {
                foreach (var ufo in ufos)
                {
                    var s = string.Format("ClientOrderID: {0} # T:{1} # S:{2} # P:{3} # Q:{4}", 
                        ufo.Key, ufo.Value.Symbol, ufo.Value.Status, ufo.Value.Price, ufo.Value.Qty);
                    Fix.Out(s);
                }
            }
            catch (Exception){}
        }
    }


    internal class Fix
    {
        static internal void Out(string o)
        {
            System.Diagnostics.Debug.WriteLine(o);
        }

        static internal void Out(Message msg)
        {
            System.Diagnostics.Debug.WriteLine(msg.ToString().Replace((char)0x01, (char)0x20));
        }
    }

    internal class Account
    {
        internal static string target = string.Empty;
        internal static string account = string.Empty;
        internal static string clientid = string.Empty;

        internal static void GetAccountInfo(SessionSettings ss)
        {
            foreach (var session in ss.getSessions())
            {
                var dict = ss.get(session as SessionID);

                target = dict.getString("TargetCompID");
                var dst = dict.getString("TargetCompID") 
                    + " : " + dict.getString("SocketConnectHost") 
                    + " : " + dict.getString("SocketConnectPort");
                Fix.Out(dst);

                var acc = dict.getString("Account");
                var cid = dict.getString("ClientID");

                if (!string.IsNullOrEmpty(acc) && !string.IsNullOrEmpty(cid))
                {
                    account = acc;
                    clientid = cid;
                }

                Fix.Out(Account.account + " / " + Account.clientid + "\n");
            }
        }
    }
}
