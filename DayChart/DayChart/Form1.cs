using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DayChart
{
    public partial class Form1 : Form
    {
        System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();

        public Form1()
        {
            InitializeComponent();

            _timer.Interval = 100;
            _timer.Tick += timer_proc;
            _timer.Enabled = true;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.Multiselect = false;
            dlg.ReadOnlyChecked = true;
            dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

            if (DialogResult.OK != dlg.ShowDialog())
            {
                return ;
            }

            chart1.Clear();

            using (var sr = File.OpenText(dlg.FileName))
            {
                for (string line = sr.ReadLine(); null != line; line = sr.ReadLine())
                {
                    chart1.Add(new TOHLCV(line));
                }
            }
        }

        List<TOHLCV> tmp = new List<TOHLCV>(); 
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.CheckFileExists = true;
            dlg.CheckPathExists = true;
            dlg.Multiselect = false;
            dlg.ReadOnlyChecked = true;
            dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

            if (DialogResult.OK != dlg.ShowDialog())
            {
                return;
            }

            chart1.Clear();
            chart1.Invalidate();

            using (var sr = File.OpenText(dlg.FileName))
            {
                for (string line = sr.ReadLine(); null != line; line = sr.ReadLine())
                {
                    tmp.Add(new TOHLCV(line));
                }
            }

            _timer.Start();
        }

        private void timer_proc(object sender, EventArgs e)
        {
            if (tmp.Count > 0)
            {
                chart1.Add(tmp[0]);
                tmp.RemoveAt(0);
            }
            else
            {
                _timer.Stop();
            }
        }
    }
}
