using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;

namespace FixEngine
{
    internal class FixAppKingStar : FixApplication
    {
        public sealed override string ProcLimitOrder(ref bool comp, string symbol, string price, int qty)
        {
            var isBuy = IsBuy(qty);
            var pos = base.GetOpenPosition(symbol);

            if (IsOpen(pos, qty))
            {
                return SendNewOrder(isBuy, true, symbol, price, Math.Abs(qty).ToString());
            }
            else if (IsClose(pos, qty))
            {
                return SendNewOrder(isBuy, false, symbol, price, Math.Abs(qty).ToString());
            }
            else if (IsTransSide(pos, qty))
            {
                SendNewOrder(isBuy, false, symbol, price, Math.Abs(pos).ToString());
                qty += pos;
                return SendNewOrder(isBuy, true, symbol, price, Math.Abs(qty).ToString());
            }

            return string.Empty;
        }

        public sealed override string ProcCancelOrder(ref bool comp, string symbol, string id, int qty)
        {
            return CancelOrder(IsBuy(qty), symbol, id);
        }

        private string SendNewOrder(bool isbuy, bool isopen, string symbol, string price, string qty)
        {
            var sno = MessageFactory.NewOrderSingle(TradingSession());
            var future = string.Empty;
            var contract = string.Empty;
            var maturity = string.Empty;
            var exchange = string.Empty;

            if (sno == null || !SplitSymbol(symbol, ref future, ref contract, ref maturity, ref exchange))
            {
                return string.Empty;
            }

            var ClOrdID = AllocClOrdID(symbol);
            var open = isopen ? "O" : "C";
            var buy = isbuy ? SideBuy : SideSell;

            sno.setField(1, Account.account);
            sno.setField(109, Account.clientid);
            Fix.Out("tag207 : SecurityExchange = " + exchange);
            sno.setField(207, exchange);
            Fix.Out("tag 77 : OpenClose = " + open);
            sno.setField(77, open);
            Fix.Out("tag 11 : ClOrdID = " + ClOrdID);
            sno.setField(11, ClOrdID);
            Fix.Out("tag40 : OrdType = 2(Limit)");
            sno.setField(40, "2");
            Fix.Out("tag54 : side(1=buy,2=sell) = " + buy);
            sno.setField(54, buy);
            sno.setField(21, "1");
            Fix.Out("tag55 : Symbol = " + contract);
            sno.setField(55, contract);
            Fix.Out("tag38 : OrderQty = " + qty);
            sno.setField(38, qty);
            Fix.Out("tag44 : Price = " + price);
            sno.setField(44, price);
            sno.setField(60, GetTimeString());

            return SendMessageToSession(sno) ? ClOrdID : string.Empty;
        }

        private string CancelOrder(bool isbuy, string symbol, string id)
        {
            var co = MessageFactory.OrderCancelRequest(TradingSession());
            var future = string.Empty;
            var contract = string.Empty;
            var maturity = string.Empty;
            var exchange = string.Empty;

            if (co == null || !SplitSymbol(symbol, ref future, ref contract, ref maturity, ref exchange))
            {
                return string.Empty;
            }

            var ClOrdID = AllocClOrdID(symbol);
            var buy = isbuy ? SideBuy : SideSell;

            Fix.Out("tag 11 : ClOrdID = " + ClOrdID);
            co.setField(11, ClOrdID);
            Fix.Out("tag41 : OrigClOrdID = " + id);
            co.setField(41, id);
            Fix.Out("tag54 : Side(1=buy,2=sell) = " + buy);
            co.setField(54, buy);
            Fix.Out("tag55 : Symbol = " + contract);
            co.setField(55, contract);
            co.setField(60, GetTimeString());
            Fix.Out("tag1 : Account = " + Account.account);
            co.setField(1, Account.account);

            return SendMessageToSession(co) ? ClOrdID : string.Empty;
        }

        private string QueryOrder(bool isbuy, string symbol, string id)
        {
            var qo = MessageFactory.OrderStatusRequest(TradingSession());
            if (qo == null)
            {
                return string.Empty;
            }

            var ClOrdID = AllocClOrdID(symbol);
            var buy = isbuy ? SideBuy : SideSell;

            Fix.Out("tag11 : OrderID = " + id);
            qo.setField(11, id);
            Fix.Out("tag54 : Side(1=buy,2=sell) = " + buy);
            qo.setField(54, buy);
            Fix.Out("tag55 : Symbol = " + symbol);
            qo.setField(55, symbol);

            return SendMessageToSession(qo) ? ClOrdID : string.Empty;
        }
    }
}
