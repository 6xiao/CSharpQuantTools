namespace TestDlg
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.start = new System.Windows.Forms.Button();
            this.stop = new System.Windows.Forms.Button();
            this.testLogon = new System.Windows.Forms.Button();
            this.tbCmd = new System.Windows.Forms.TextBox();
            this.msgid = new System.Windows.Forms.Label();
            this.Send = new System.Windows.Forms.Button();
            this.lastMsg = new System.Windows.Forms.RichTextBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.lbconfig = new System.Windows.Forms.Label();
            this.tbcfg = new System.Windows.Forms.TextBox();
            this.LimitOrder = new System.Windows.Forms.Button();
            this.deals = new System.Windows.Forms.Button();
            this.Position = new System.Windows.Forms.Button();
            this.market = new System.Windows.Forms.Button();
            this.cr = new System.Windows.Forms.CheckBox();
            this.cancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // start
            // 
            this.start.Location = new System.Drawing.Point(15, 14);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(75, 23);
            this.start.TabIndex = 0;
            this.start.Text = "start";
            this.start.UseVisualStyleBackColor = true;
            this.start.Click += new System.EventHandler(this.start_Click);
            // 
            // stop
            // 
            this.stop.Location = new System.Drawing.Point(105, 14);
            this.stop.Name = "stop";
            this.stop.Size = new System.Drawing.Size(75, 23);
            this.stop.TabIndex = 1;
            this.stop.Text = "stop";
            this.stop.UseVisualStyleBackColor = true;
            this.stop.Click += new System.EventHandler(this.stop_Click);
            // 
            // testLogon
            // 
            this.testLogon.Location = new System.Drawing.Point(192, 14);
            this.testLogon.Name = "testLogon";
            this.testLogon.Size = new System.Drawing.Size(75, 23);
            this.testLogon.TabIndex = 2;
            this.testLogon.Text = "testLogon";
            this.testLogon.UseVisualStyleBackColor = true;
            this.testLogon.Click += new System.EventHandler(this.testLogon_Click);
            // 
            // tbCmd
            // 
            this.tbCmd.Location = new System.Drawing.Point(15, 79);
            this.tbCmd.Name = "tbCmd";
            this.tbCmd.Size = new System.Drawing.Size(961, 21);
            this.tbCmd.TabIndex = 3;
            this.tbCmd.Text = "{}";
            // 
            // msgid
            // 
            this.msgid.AutoSize = true;
            this.msgid.Location = new System.Drawing.Point(290, 3);
            this.msgid.Name = "msgid";
            this.msgid.Size = new System.Drawing.Size(263, 12);
            this.msgid.TabIndex = 4;
            this.msgid.Text = "我左操作，右发送，记录在腰间，消息在胸间！ ";
            // 
            // Send
            // 
            this.Send.Location = new System.Drawing.Point(556, 17);
            this.Send.Name = "Send";
            this.Send.Size = new System.Drawing.Size(62, 23);
            this.Send.TabIndex = 5;
            this.Send.Text = "send";
            this.Send.UseVisualStyleBackColor = true;
            this.Send.Click += new System.EventHandler(this.Send_Click);
            // 
            // lastMsg
            // 
            this.lastMsg.Location = new System.Drawing.Point(17, 107);
            this.lastMsg.Name = "lastMsg";
            this.lastMsg.ReadOnly = true;
            this.lastMsg.Size = new System.Drawing.Size(967, 42);
            this.lastMsg.TabIndex = 6;
            this.lastMsg.Text = "";
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.HorizontalExtent = 2000;
            this.listBox1.HorizontalScrollbar = true;
            this.listBox1.ItemHeight = 12;
            this.listBox1.Location = new System.Drawing.Point(0, 160);
            this.listBox1.Name = "listBox1";
            this.listBox1.ScrollAlwaysVisible = true;
            this.listBox1.Size = new System.Drawing.Size(1006, 280);
            this.listBox1.TabIndex = 7;
            // 
            // lbconfig
            // 
            this.lbconfig.AutoSize = true;
            this.lbconfig.Location = new System.Drawing.Point(15, 55);
            this.lbconfig.Name = "lbconfig";
            this.lbconfig.Size = new System.Drawing.Size(77, 12);
            this.lbconfig.TabIndex = 8;
            this.lbconfig.Text = "配置文件路径";
            // 
            // tbcfg
            // 
            this.tbcfg.Location = new System.Drawing.Point(106, 55);
            this.tbcfg.Name = "tbcfg";
            this.tbcfg.Size = new System.Drawing.Size(878, 21);
            this.tbcfg.TabIndex = 9;
            // 
            // LimitOrder
            // 
            this.LimitOrder.Location = new System.Drawing.Point(627, 15);
            this.LimitOrder.Name = "LimitOrder";
            this.LimitOrder.Size = new System.Drawing.Size(75, 25);
            this.LimitOrder.TabIndex = 10;
            this.LimitOrder.Text = "LimitOrder";
            this.LimitOrder.UseVisualStyleBackColor = true;
            this.LimitOrder.Click += new System.EventHandler(this.LimitOrder_Click);
            // 
            // deals
            // 
            this.deals.Location = new System.Drawing.Point(861, 16);
            this.deals.Name = "deals";
            this.deals.Size = new System.Drawing.Size(49, 25);
            this.deals.TabIndex = 11;
            this.deals.Text = "Deals";
            this.deals.UseVisualStyleBackColor = true;
            this.deals.Click += new System.EventHandler(this.deals_Click);
            // 
            // Position
            // 
            this.Position.Location = new System.Drawing.Point(916, 16);
            this.Position.Name = "Position";
            this.Position.Size = new System.Drawing.Size(70, 25);
            this.Position.TabIndex = 12;
            this.Position.Text = "position";
            this.Position.UseVisualStyleBackColor = true;
            this.Position.Click += new System.EventHandler(this.Position_Click);
            // 
            // market
            // 
            this.market.Location = new System.Drawing.Point(709, 16);
            this.market.Name = "market";
            this.market.Size = new System.Drawing.Size(86, 25);
            this.market.TabIndex = 13;
            this.market.Text = "MarketOrder";
            this.market.UseVisualStyleBackColor = true;
            this.market.Click += new System.EventHandler(this.market_Click);
            // 
            // cr
            // 
            this.cr.AutoSize = true;
            this.cr.Location = new System.Drawing.Point(294, 33);
            this.cr.Name = "cr";
            this.cr.Size = new System.Drawing.Size(156, 16);
            this.cr.TabIndex = 14;
            this.cr.Text = "崩溃重启，重发成交信息";
            this.cr.UseVisualStyleBackColor = true;
            // 
            // cancel
            // 
            this.cancel.Location = new System.Drawing.Point(801, 18);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(54, 23);
            this.cancel.TabIndex = 15;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1006, 440);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.cr);
            this.Controls.Add(this.market);
            this.Controls.Add(this.Position);
            this.Controls.Add(this.deals);
            this.Controls.Add(this.LimitOrder);
            this.Controls.Add(this.tbcfg);
            this.Controls.Add(this.lbconfig);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.lastMsg);
            this.Controls.Add(this.Send);
            this.Controls.Add(this.msgid);
            this.Controls.Add(this.tbCmd);
            this.Controls.Add(this.testLogon);
            this.Controls.Add(this.stop);
            this.Controls.Add(this.start);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button start;
        private System.Windows.Forms.Button stop;
        private System.Windows.Forms.Button testLogon;
        private System.Windows.Forms.TextBox tbCmd;
        private System.Windows.Forms.Label msgid;
        private System.Windows.Forms.Button Send;
        private System.Windows.Forms.RichTextBox lastMsg;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Label lbconfig;
        private System.Windows.Forms.TextBox tbcfg;
        private System.Windows.Forms.Button LimitOrder;
        private System.Windows.Forms.Button deals;
        private System.Windows.Forms.Button Position;
        private System.Windows.Forms.Button market;
        private System.Windows.Forms.CheckBox cr;
        private System.Windows.Forms.Button cancel;
    }
}

