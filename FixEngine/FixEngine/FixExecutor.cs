using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickFix;

namespace FixEngine
{
    public delegate void FixCallBack(string RspMsg);
    internal delegate void FixReportMessage(int msgType, string id, string connect);

    public class FixExecutor
    {
        private CommandProcess cmdproc = null;
        private SocketInitiator sock = null;
        private event FixCallBack eventCallBack;
        
        public void Start(string settingFile, bool ResendResult)
        {
            Stop();

            var settings = new SessionSettings(settingFile);
            Account.GetAccountInfo(settings);

            if (ResendResult) foreach (var session in settings.getSessions())
            {
                var dict = settings.get(session as SessionID);

                var target = dict.getString("FileStorePath") + "\\"
                    + dict.getString("BeginString") + "-"
                    + dict.getString("SenderCompID") + "-"
                    + dict.getString("TargetCompID") + ".seqnums";

                try
                {
                    var s = System.IO.File.ReadAllText(target);
                    var d = s.Remove(s.Length - 10);
                    System.IO.File.WriteAllText(target, d + "0000000001");
                }
                catch (Exception){}
            }

            cmdproc = SessionFactory.CommandProcessInstance(Account.target, this.AppReport);
            sock = new SocketInitiator(cmdproc.App, new FileStoreFactory(settings), settings, new DefaultMessageFactory());
            sock.start();
        }

        public void AddCallBack(FixCallBack fcb)
        {
            eventCallBack += fcb;
        }

        public void RemoveCallBack(FixCallBack fcb)
        {
            eventCallBack -= fcb;
        }

        public bool IsLogon()
        {
            return sock != null && sock.isLoggedOn();
        }

        public void Stop()
        {
            if (sock != null)
            {
                sock.stop();
                sock.Dispose();
                sock = null;
            }
        }

        private void AppReport(string RspMsg)
        {
            if (eventCallBack != null)
            {
                eventCallBack(RspMsg);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns>2,同步完成；1,处理过，等回复；0,不处理；-1,处理出错；</returns>
        public int DealCommand(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                AppReport("DealCommand : command is nil");
                return -1;
            }

            if (sock != null && sock.isLoggedOn())
            {
                return cmdproc.DealCommand(cmd);
            }

            AppReport("Session : disconnect or connecting");
            return -1;
        }
    }
}
