using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;
using QuickFix40;
using Message = QuickFix.Message;

namespace FixEngine
{
    public class FixAppYuanTa : FixApplication
    {

        public static string GenerateYuandaContract(string contract, string maturity)
        {
            string [] monthTable={string.Empty,"A","B","C","D","E","F","G","H","I","J","K","L"};
            int maturityNumber=0;
            bool maturityIsValid = int.TryParse(maturity, out maturityNumber);
            int maturityBase = 200000;
            int maturityYear=0,maturityMonth = 0;
            string monthCode=string.Empty;

            if (string.IsNullOrWhiteSpace(contract))
                return string.Empty;

            if (maturityIsValid && maturityNumber >= 201201 && maturityNumber < 205001)
            {
                int maturityYearMon = maturityNumber - maturityBase;
                maturityYear = maturityYearMon /100 %10;
                maturityMonth = maturityYearMon % 100;
                if(maturityYear>=1 && maturityYear<=9 && maturityMonth>=1 && maturityMonth<=12)
                {
                    return contract+monthTable[maturityMonth]+maturityYear;
                }
                
            }
            
            return string.Empty;
        }
        public sealed override string ProcLimitOrder(ref bool comp, string symbol, string price, int qty)
        {
            return SendNewOrder(IsBuy(qty), true, symbol, price, Math.Abs(qty).ToString());
        }

        public sealed override string ProcMarketOrder(ref bool comp, string symbol, string price, int qty)
        {
            return SendNewOrder(IsBuy(qty), false, symbol, price, Math.Abs(qty).ToString());
        }

        public sealed override string ProcCancelOrder(ref bool comp, string symbol, string id, int qty)
        {
            return CancelOrder(IsBuy(qty), symbol, id, Math.Abs(qty).ToString());
        }

        private string SendNewOrder(bool isbuy, bool isLimit, string symbol, string price, string qty)
        {
            var sno = MessageFactory.NewOrderSingle(TradingSession());
            var npid = MessageFactory.NoPartyIDs(TradingSession());
            var future = string.Empty;
            var contract = string.Empty;
            var maturity = string.Empty;
            var exchange = string.Empty;

            if (sno == null || npid == null
                || !SplitSymbol(symbol, ref future, ref contract, ref maturity, ref exchange))
            {
                return string.Empty;
            }
            
            var ClOrdID = AllocClOrdID(symbol);
            var buy = isbuy ? SideBuy : SideSell;
            var ordtype = isLimit ? LimitOrder : MarketOrder;
            var tif = isLimit ? "0" : "4";//4 = Fill or Kill (FOK)
//            var tif = isLimit ? "0" : "3";//3 = Immediate or Cancel (IOC)
            string s = GenerateYuandaContract(contract, maturity);
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            sno.setField(11, ClOrdID);
            sno.setField(40, ordtype);
            sno.setField(54, buy);
            sno.setField(21, "1");
            sno.setField(55, s);
            sno.setField(200, maturity);
            sno.setField(38, qty);
            sno.setField(44, price);
            sno.setField(59, tif);
            sno.setField(60, GetTimeString());

            sno.setField(22, "100"); //SecurityIDSource
            sno.setField(461, "FXXXXX"); //CFICode
           
            npid.setField(447, "D");
            npid.setField(452, "5");
            npid.setField(448, Account.account);
            sno.addGroup(npid);
 
            return SendMessageToSession(sno) ? ClOrdID : string.Empty;
        }

        private string CancelOrder(bool isBuy, string symbol, string id, string qty)
        {
            var co = MessageFactory.OrderCancelRequest(TradingSession());
            var npid = MessageFactory.NoPartyIDs(TradingSession());
            var future = string.Empty;
            var contract = string.Empty;
            var maturity = string.Empty;
            var exchange = string.Empty;

            if (co == null || npid == null
                || !SplitSymbol(symbol, ref future, ref contract, ref maturity, ref exchange))
            {
                return string.Empty;
            }

            var ClOrdID = AllocClOrdID(symbol);
            var buy = isBuy ? SideBuy : SideSell;

            string s = GenerateYuandaContract(contract, maturity);

            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            co.setField(11, ClOrdID);
            co.setField(41, id);
            co.setField(55, s);
            co.setField(200, maturity);
            co.setField(38, qty);
            co.setField(54, buy);
            co.setField(60, GetTimeString());

            co.setField(22, "100");
            co.setField(461, "FXXXXX");

            npid.setField(447, "D");
            npid.setField(452, "5");
            npid.setField(448, Account.account);
            co.addGroup(npid);

            return SendMessageToSession(co) ? ClOrdID : string.Empty;
        }
    }
}
