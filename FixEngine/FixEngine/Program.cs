using System;
using System.Diagnostics;
using System.IO;
using QuickFix;

namespace FixEngine
{
    class Program
    {
        static void Om(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        [STAThread]
        static void Main(string[] args)
        {
            var cp = Process.GetCurrentProcess().MainModule.FileName;
            Directory.SetCurrentDirectory(Path.GetDirectoryName(cp));
            
            try
            {
                var fe = new FixExecutor();
                fe.AddCallBack(Om);
                fe.AddCallBack(Om);
                fe.Start(@"E:\FixEngine\FixEngine\myserver.cfg");

                for ( ; ;  )
                {
                    Console.WriteLine("quit stop goon buy sell cbuy csell qbuy qsell?");

                    var cmd = Console.ReadLine();//.ToLower();

                    if ("quit" == cmd)
                    {
                        break;
                    }
                    
                    Console.WriteLine(fe.DealCommand(cmd));

                    if ("buy" == cmd)
                    {
                        var open = Console.ReadLine();
                        if (open == "open" || open == "close")
                        {
//                            application.SendNewOrder(true, open == "open", "cffe", "IF1112", Console.ReadLine(), Console.ReadLine());
                        }
                    }
                    else if ("sell" == cmd)
                    {
                        var open = Console.ReadLine();
                        if (open == "open" || open == "close")
                        {
//                            application.SendNewOrder(false, open == "open", "cffe", "IF1112", Console.ReadLine(), Console.ReadLine());
                        }
                    }
                    else if ("cbuy" == cmd)
                    {
//                        application.CancelOrder(true, "IF1112", Console.ReadLine());
                    }
                    else if ("csell" == cmd)
                    {
//                        application.CancelOrder(false, "IF1112", Console.ReadLine());
                    }
                    else if ("qbuy" == cmd)
                    {
//                        application.QueryOrder(true, "IF1112", Console.ReadLine());
                    }
                    else if ("qsell" == cmd)
                    {
//                        application.QueryOrder(false, "IF1112", Console.ReadLine());
                    }
                }

                fe.Stop();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Fix.Out(e.ToString());
                Console.ReadLine();
            }
            finally
            {
                Fix.Out("AtExit");
            }
        }
    }
}
