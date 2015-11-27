using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using QuickFix;
using FixEngine;

namespace TestDlg
{
    public partial class Form1 : Form
    {
        FixExecutor fe = new FixExecutor();
        private int mid = 55;
        private int GetMid()
        {
            return ++mid;
        }

        void Outmessage(string msg)
        {
            if (!listBox1.InvokeRequired)
            {
                System.Diagnostics.Debug.WriteLine("report : " + msg);

                for (; listBox1.Items.Count > 1024; listBox1.Items.RemoveAt(0)) ;
                listBox1.Items.Add(msg);
            }
            else
            {
                this.BeginInvoke(new FixCallBack(Outmessage), new object[] { msg });
            }
        }

        public Form1()
        {
            InitializeComponent();
            fe.AddCallBack(Outmessage);

            tbcfg.Text = System.IO.Directory.GetCurrentDirectory();
            tbcfg.Text += "\\FixConfig\\.cfg";
        }

        private void start_Click(object sender, EventArgs e)
        {
            if (!System.IO.File.Exists(tbcfg.Text))
            {
                MessageBox.Show("配置文件不存在");
                return;
            }

            //Start(配置文件路径，是否重发应用消息）
            //是否重发应用消息(True/False): 通过重置序列号来请求网关重发当天的应用消息，包括成交、拒绝、撤单
            fe.Start(tbcfg.Text, cr.Checked);
        }

        private void stop_Click(object sender, EventArgs e)
        {
            fe.Stop();
        }

        private void testLogon_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Ping / IsLogon: " + fe.IsLogon().ToString());
        }
        
        private void Send_Click(object sender, EventArgs e)
        {
            var header = string.Format("Source=TestDlg,Destination=Fix,MessageId={0}", GetMid());
            var msg = header + tbCmd.Text;

            lastMsg.Text = msg;
            Outmessage(msg);
            fe.DealCommand(msg);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            fe.Stop();
        }

        private void LimitOrder_Click(object sender, EventArgs e)
        {
            tbCmd.Text = "{SendLimitOrder,TickerName=,Price=,Quantity=}";
        }

        private void deals_Click(object sender, EventArgs e)
        {
            tbCmd.Text = "{RequestDeal}";
        }

        private void Position_Click(object sender, EventArgs e)
        {
            tbCmd.Text = "{RequestOpenPositions}";
        }

        private void market_Click(object sender, EventArgs e)
        {
            tbCmd.Text = "{SendMarketOrder,TickerName=,Price=,Quantity=}";
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            tbCmd.Text = "{CancelLimitOrder,TickerName=,InternalOrderId=}";
        }
    }
}
