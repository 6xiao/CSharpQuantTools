using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;

namespace FixEngine
{
    class MessageFactory
    {
        internal static dynamic NewOrderSingle(SessionID sid)
        {
            if (sid == null)
            {
                return null;
            }

            string value = sid.getBeginString();

            if (value.Equals("FIX.4.0"))
                return new QuickFix40.NewOrderSingle();
            
            if (value.Equals("FIX.4.1"))
                return new QuickFix41.NewOrderSingle();

            if (value.Equals("FIX.4.2"))
                return new QuickFix42.NewOrderSingle();

            if (value.Equals("FIX.4.3"))
                return new QuickFix43.NewOrderSingle();

            if (value.Equals("FIX.4.4"))
                return new QuickFix44.NewOrderSingle();

            if (value.Equals("FIX.5.0"))
                return new QuickFix50.NewOrderSingle();

            return null;
        }

        internal static dynamic NoPartyIDs(SessionID sid)
        {
            if (sid == null)
            {
                return null;
            }

            string value = sid.getBeginString();

            if (value.Equals("FIX.4.3"))
                return new QuickFix43.NewOrderSingle.NoPartyIDs();

            if (value.Equals("FIX.4.4"))
                return new QuickFix44.NewOrderSingle.NoPartyIDs();

            if (value.Equals("FIX.5.0"))
                return new QuickFix50.NewOrderSingle.NoPartyIDs();

            return null;
        }

        internal static dynamic OrderCancelRequest(SessionID sid)
        {
            if (sid == null)
            {
                return null;
            }

            string value = sid.getBeginString();

            if (value.Equals("FIX.4.0"))
                return new QuickFix40.OrderCancelRequest();

            if (value.Equals("FIX.4.1"))
                return new QuickFix41.OrderCancelRequest();

            if (value.Equals("FIX.4.2"))
                return new QuickFix42.OrderCancelRequest();

            if (value.Equals("FIX.4.3"))
                return new QuickFix43.OrderCancelRequest();

            if (value.Equals("FIX.4.4"))
                return new QuickFix44.OrderCancelRequest();

            if (value.Equals("FIX.5.0"))
                return new QuickFix50.OrderCancelRequest();

            return null;            
        }

        internal static dynamic OrderStatusRequest(SessionID sid)
        {
            if (sid == null)
            {
                return null;
            }

            string value = sid.getBeginString();

            if (value.Equals("FIX.4.0"))
                return new QuickFix40.OrderStatusRequest();

            if (value.Equals("FIX.4.1"))
                return new QuickFix41.OrderStatusRequest();

            if (value.Equals("FIX.4.2"))
                return new QuickFix42.OrderStatusRequest();

            if (value.Equals("FIX.4.3"))
                return new QuickFix43.OrderStatusRequest();

            if (value.Equals("FIX.4.4"))
                return new QuickFix44.OrderStatusRequest();

            if (value.Equals("FIX.5.0"))
                return new QuickFix50.OrderStatusRequest();

            return null;
        }
    }
}
