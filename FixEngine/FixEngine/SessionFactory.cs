using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FixEngine
{
    class SessionFactory
    {
        internal static dynamic CommandProcessInstance(string target, FixCallBack report)
        {
            if (!string.IsNullOrEmpty(target)) 
            switch (target)
            {
                case "NHQH":
                case "GLQH":
                case "NANHUA":
                case "SUNGARD":
                    return new CommandProcess(report, new FixAppKingStar());//国内金仕达

                case "YUTAFOMD3":
                    return new CommandProcess(report, new FixAppYuanTa());//台湾元大
                
                case "IB":
                    return new CommandProcess(report, new FixAppIB());//IB

                default:
                    break;
            }

            return new CommandProcess(report, new FixApplication());
        }
    }
}
