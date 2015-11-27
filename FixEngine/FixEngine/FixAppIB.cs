using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
namespace FixEngine
{
    public class FixAppIB:FixApplication
    {
        #region fix message tags and constants
        /// <summary>
        /// IB FIX CTCI用到的TAG 定义
        /// </summary>
        enum EFixTags
        {
            /// <summary>
            /// 11
            /// </summary>
            ClOrdID = 11,
            /// <summary>
            /// 21
            /// </summary>
            HandInst = 21, 
            /// <summary>
            /// 38
            /// </summary>
            OrderQty = 38,
            /// <summary>
            /// 40
            /// </summary>
            OrderType = 40,
            /// <summary>
            /// 41
            /// </summary>
            OrigClOrderID=41,
            /// <summary>
            /// 44
            /// </summary>
            Price=44,
            /// <summary>
            /// 47
            /// </summary>
            OrderCapacity=47,
            /// <summary>
            /// 54
            /// </summary>
            Side = 54, 
            /// <summary>
            /// 55
            /// </summary>
            Symbol = 55, 
            /// <summary>
            /// 59
            /// </summary>
            TimeInForce=59,
            /// <summary>
            /// 100
            /// </summary>
            ExDestination = 100, 
            /// <summary>
            /// 167
            /// </summary>
            SecurityType = 167, 
            /// <summary>
            /// 200
            /// </summary>
            Expiration=200,
            /// <summary>
            /// 204
            /// </summary>
            CustomerOrFirm=204,
            /// <summary>
            /// 207
            /// </summary>
            SecurityExchange = 207,
            /// <summary>
            /// 440
            /// </summary>
            ClearingAccount=440,
            /// <summary>
            /// 6122
            /// </summary>
            OptionAcct=6122

        };
        
        /// <summary>
        /// 从TPC,OE认可的格式到IB认可格式的转换
        /// </summary>
        private readonly Dictionary<string, string> acronymMap = new Dictionary<string, string>()
        {
            {"Futures","FUT"}
        };
        /// <summary>
        /// OrderType: 2
        /// </summary>
        private const string LIMIT = "2";
        /// <summary>
        /// OrderType: 1
        /// </summary>
        private const string MARKET = "1";
        /// <summary>
        ///Side: 1
        /// </summary>
        private const string BUY = "1";
        /// <summary>
        /// Side: 2
        /// </summary>
        private const string SELL = "2";
        /// <summary>
        /// TimeInForce:4
        /// </summary>
        private const string FILLORKILL = "4";
        /// <summary>
        /// TimeInForce:3
        /// </summary>
        private const string IOC = "3";
        /// <summary>
        /// TimeInForce:0
        /// </summary>
        private const string DAY = "0";
        /// <summary>
        /// CustomerOrFirm:0
        /// </summary>
        private const string CUSTOMER = "0";
        #endregion

        public override string ProcLimitOrder(ref bool comp, string symbol, string price, int qty)
        {

            QuickFix.Message sno = generateOrderMessage(symbol, price, qty, true);
            if (sno == null) return string.Empty;
            return SendMessageToSession(sno) ? sno.getField((int)EFixTags.ClOrdID) : string.Empty;
        }

        public override string ProcMarketOrder(ref bool comp, string symbol, string price, int qty)
        {
            QuickFix.Message sno = generateOrderMessage(symbol, price, qty, false);
            if (sno == null) return string.Empty;
            return SendMessageToSession(sno) ? sno.getField((int)EFixTags.ClOrdID) : string.Empty;
        }

        private Dictionary<string, IBOrderInfo> orderTypeList = new Dictionary<string, IBOrderInfo>();
        public override string ProcCancelOrder(ref bool comp, string symbol, string id, int qty)
        {
            QuickFix.Message co = (QuickFix.Message)MessageFactory.OrderCancelRequest(TradingSession());
            string SecType = string.Empty, IndexSymbol = string.Empty, expiry = string.Empty, exchangeMarket = string.Empty;
            if (!SplitSymbol(symbol, ref SecType, ref IndexSymbol, ref expiry, ref exchangeMarket))
            {
                return string.Empty;
            }
            if (co == null)
            {
                return string.Empty;
            }

            Dictionary<EFixTags, string> cancelOrderTags = new Dictionary<EFixTags, string>();
            Debug.Assert(orderTypeList.ContainsKey(id), "Unable to cancel Not Exist Order Id: " + id);
            if(orderTypeList.ContainsKey(id))
            {
                cancelOrderTags[EFixTags.OrigClOrderID] = id;
                cancelOrderTags[EFixTags.ClOrdID] = orderTypeList[id].mainOrderId + "."+(++orderTypeList[id].subOrderId);
                cancelOrderTags[EFixTags.Symbol] = IndexSymbol;
                cancelOrderTags[EFixTags.HandInst] = "2";
                cancelOrderTags[EFixTags.Side] = IsBuy(qty) ? BUY : SELL;
                cancelOrderTags[EFixTags.OrderQty] = Math.Abs(qty).ToString();
                cancelOrderTags[EFixTags.OrderType] = orderTypeList[id].IsLimit?LIMIT:MARKET;
                fillFixMessageStructure(cancelOrderTags, ref co);
                
                return SendMessageToSession(co) ? cancelOrderTags[EFixTags.ClOrdID] : string.Empty;
            }
            else
            {
                return string.Empty;
            }
            
        }

        private QuickFix.Message generateOrderMessage(string symbol,string price,int qty,bool isLimit)
        {
            string SecType = string.Empty, IndexSymbol = string.Empty, expiry = string.Empty, exchangeMarket = string.Empty;
            if (!SplitSymbol(symbol,ref SecType, ref IndexSymbol,ref expiry,ref exchangeMarket))
            {
                return null;
            }

            SecType= acronymMap[SecType];
            
            QuickFix.Message sno = (QuickFix.Message)MessageFactory.NewOrderSingle(TradingSession());
            if (sno == null)
            {
                return null;
            }
            
            Dictionary<EFixTags, string> limitOrderTags = new Dictionary<EFixTags, string>();
            var ClOrdID = AllocClOrdID(symbol);
            
            //为了与TWS联动，需要生成特别格式的ORDERID
            limitOrderTags[EFixTags.ClOrdID] = ClOrdID+".0";
            
            limitOrderTags[EFixTags.Symbol] = IndexSymbol;
            limitOrderTags[EFixTags.HandInst] = "2";
            limitOrderTags[EFixTags.Side] = IsBuy(qty) ? BUY : SELL;
            if(isLimit)
            limitOrderTags[EFixTags.Price] = price;
            limitOrderTags[EFixTags.OrderQty] = Math.Abs(qty).ToString();
            limitOrderTags[EFixTags.Expiration] = expiry;
            limitOrderTags[EFixTags.OrderType] = isLimit?LIMIT:MARKET;
            limitOrderTags[EFixTags.SecurityType] = SecType;
            limitOrderTags[EFixTags.SecurityExchange] = exchangeMarket;
            limitOrderTags[EFixTags.ExDestination] = exchangeMarket;
            limitOrderTags[EFixTags.CustomerOrFirm] = CUSTOMER;
            limitOrderTags[EFixTags.TimeInForce] = isLimit?DAY:IOC;
            //limitOrderTags[EFixTags.OrderCapacity] = "I";
            limitOrderTags[EFixTags.ClearingAccount] = "U00403";
            //limitOrderTags[EFixTags.OptionAcct] = "c";
            fillFixMessageStructure(limitOrderTags, ref sno);

            orderTypeList.Add(ClOrdID+".0", 
                new IBOrderInfo() { 
                    IsLimit = isLimit, 
                    mainOrderId = ClOrdID,
                    subOrderId=0,
                    Quantity=qty,
                    Price=price,
                    TickerName=symbol
                });
            return sno;
        }
        
        private static void fillFixMessageStructure(Dictionary<EFixTags,string> tagMap, ref QuickFix.Message fixMessage)
        {
            foreach (KeyValuePair<EFixTags, string> kvp in tagMap)
            {
                Fix.Out(string.Format("Tag {0}: {1}={2}", (int)kvp.Key, kvp.Key.ToString(), kvp.Value));
                fixMessage.setField((int)kvp.Key, kvp.Value);
            }
        }


        private string lastClOrderID = string.Empty;
        /// <summary>
        /// 重载基类，IB FIX下单如果需要可以由TWS观察控制，需要有特殊格式的ClOrderID
        /// 格式为: xxx xxx为数字
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        protected override string AllocClOrdID(string symbol)
        {
            lock (lastClOrderID)
            {
               string s = DateTime.Now.ToString("Hmmssfff");
               if (s == lastClOrderID)
               {
                   s = DateTime.Now.AddMilliseconds(1).ToString("Hmmssfff");
               }
               lastClOrderID = s;
               return lastClOrderID;
            }
            
        }

    }

    class IBOrderInfo
    {
        public string mainOrderId;
        public int subOrderId;
        public bool IsLimit;
        public int Quantity;
        public string TickerName;
        public string Price;
        //public EIBOrderStatus OrderStatus;

    }

    /*enum EIBOrderStatus
    {
        Unhandled,Queued,Rejected,Cancelled,Filled,PartiallyFilled
    }*/
}
