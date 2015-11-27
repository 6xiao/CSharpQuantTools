using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DayChart
{
    public partial class Chart : UserControl
    {
        readonly ToolTip _tip = new ToolTip();
        readonly DrawChart _dc = new DrawChart();

        public Chart()
        {
            InitializeComponent();
        }

        private void Chart_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(_dc.Pad, this.ClientRectangle, 0, 0, _dc.PadWidth, _dc.Pad.Height, GraphicsUnit.Pixel);
        }

        public void Add(TOHLCV rc)
        {
            _dc.AddRecord(rc);
            _dc.DrawTohlcv();
            Invalidate();
        }

        public void Add(TOHLCV[] rcArray)
        {
            foreach (var tohlcv in rcArray)
            {
                _dc.AddRecord(tohlcv);
            }
            
            _dc.DrawTohlcv();
            Invalidate();
        }

        public void Clear()
        {
            _dc.Clear();
            Invalidate();
        }

        private void Chart_MouseClick(object sender, MouseEventArgs e)
        {
            _tip.Show(_dc.GetSelect(e.X * _dc.PadWidth / this.Size.Width), this);
            Invalidate();
        }

        private void Chart_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
    }

    public class DrawChart
    {
        private const int MaxCapa = 1000;
        private const int MidOffset = 2;
        private const int ItemWidth = MidOffset * 2 + 1;
        private const int PriceWidth = MaxCapa * ItemWidth;
        private const int PriceHeight = PriceWidth * 100 / MaxCapa;
        private const int VolumeHeight = PriceWidth * 50 / MaxCapa;
        private const int BmpHeight = PriceHeight + VolumeHeight;
        private const int Rule = 60;

        private static readonly Bitmap _pad = new Bitmap(PriceWidth + Rule, BmpHeight);
        private static readonly Rectangle _clip = new Rectangle(0, 0, _pad.Width, _pad.Height);
        private Graphics _graph = Graphics.FromImage(_pad);
        private List<TOHLCV> _tohlcv = new List<TOHLCV>();
        private int _DrawWidth;

        public Bitmap Pad { get { return _pad; } }
        public double HighPrice { get; private set; }
        public double LowPrice { get; private set; }
        public double MaxVolume { get; private set; }
        public int PadWidth
        {
            get
            {
                return BmpHeight > _DrawWidth ? BmpHeight + Rule : _DrawWidth + Rule;
            }
        }
        public double OpenPrice
        {
            get
            {
                foreach (var tohlcv in _tohlcv)
                {
                    if (tohlcv.Open > 0)
                    {
                        return tohlcv.Open;
                    }
                }

                return 0;
            }
        }
        

        public string GetSelect(int x)
        {
            DrawTohlcv();

            foreach (var tohlcv in _tohlcv)
            {
                if (tohlcv.RgnEnd > x)
                {
                    //var rc = new Rectangle(tohlcv.RgnEnd - ItemWidth - 1, 0, ItemWidth + 2, BmpHeight);
                    var sx = tohlcv.RgnEnd - MidOffset - 1;
                    _graph.DrawLine(Pens.Blue, sx, 0, sx, BmpHeight);

                    double lim = HighPrice - LowPrice > 0.0001
                         ? PriceHeight / (HighPrice - LowPrice)
                         : 0;
                    //double liv = MaxVolume > 0.0001 ? VolumeHeight / MaxVolume : 0;

                    //DrawVolume(tohlcv, tohlcv.RgnEnd - ItemWidth, liv);
                    //DrawPrice(tohlcv, tohlcv.RgnEnd - ItemWidth, lim);

                    var o = (int)((HighPrice - tohlcv.Open) * lim);
                    var c = (int)((HighPrice - tohlcv.Close) * lim);
                    var m = (o + c)/2;
                    _graph.FillEllipse(Brushes.Blue, tohlcv.RgnEnd - ItemWidth, m - MidOffset, ItemWidth, 2 * MidOffset);

                    return tohlcv.ToString();
                }
            }

            return string.Empty;
        }

        public void Clear()
        {
            HighPrice = 0;
            LowPrice = 0;
            MaxVolume = 0;
            _DrawWidth = 0;
            _graph.Clear(Color.White);
            _tohlcv.Clear();
        }

        public void AddRecord(TOHLCV rec)
        {
            if (rec != null && _tohlcv.Count < MaxCapa)
            {
                _tohlcv.Add(rec);

                if (rec.High > HighPrice)
                {
                    HighPrice = rec.High;
                }
                if (rec.Low < LowPrice || LowPrice == 0)
                {
                    if (rec.Low > 0)
                    {
                        LowPrice = rec.Low;
                    }
                }
                if (rec.Volume > MaxVolume)
                {
                    MaxVolume = rec.Volume;
                }
            }
        }

        public void DrawTohlcv()
        {
            double bl = 0.95;
            double lim = HighPrice - LowPrice > 0.0001
                             ? PriceHeight/(HighPrice - LowPrice)
                             : 0;
            double liv = MaxVolume > 0.0001 ? VolumeHeight/MaxVolume : 0;
            liv *= bl;

            _graph.Clear(Color.White);

            var OpenLine = (int)((HighPrice - OpenPrice) * lim);
            if (OpenPrice > 0)
            {
                if (OpenLine > 0 && OpenLine < PriceHeight)
                {
                    _graph.DrawLine(Pens.Black, 0, OpenLine, PriceWidth + Rule, OpenLine);
                    _graph.DrawString(OpenPrice.ToString("0.00"), SystemFonts.SmallCaptionFont, Brushes.Black, 0, OpenLine);
                }
            }

            if (lim > 0.0001) for (int h = 0; h < PriceHeight; ++h)
            {
                var n = Math.Abs(h - OpenLine); 
                if (n == 0 || n % Rule != 0)
                {
                    continue;
                }

                var p = HighPrice - h*(HighPrice - LowPrice)/PriceHeight;
                _graph.DrawString(p.ToString("0.00"), SystemFonts.SmallCaptionFont, 
                    p > OpenPrice ? Brushes.Red : Brushes.Green, 0, h);

                _graph.DrawLine(Pens.LightGray, Rule, h, PriceWidth + Rule, h);
            }
            
            if (liv > 0.0001) for (int m = 0; m < VolumeHeight; m += Rule)
            {
                var v = (int)((VolumeHeight - m) * MaxVolume / VolumeHeight);
                v = (int)(v/bl);
                if (v > MaxVolume) continue;
                
                _graph.DrawString(v.ToString(), SystemFonts.SmallCaptionFont, Brushes.Black, 0, PriceHeight + m);
                _graph.DrawLine(Pens.LightGray, Rule, PriceHeight + m, PriceWidth + Rule, PriceHeight + m);
            }

            _graph.DrawLine(Pens.Black, 0, PriceHeight, PriceWidth + Rule, PriceHeight);
            _graph.DrawLine(Pens.Black, Rule - 1, 0, Rule - 1, BmpHeight);

            _DrawWidth = Rule;
            int closeprice = 0;
            var closeline = new List<Point>();
            foreach (var tohlcv in _tohlcv)
            {
                DrawVolume(tohlcv, _DrawWidth, liv);

                _DrawWidth = DrawPrice(tohlcv, _DrawWidth, lim, ref closeprice);
                closeline.Add(new Point(_DrawWidth, closeprice));
            }
            if (closeline.Count > 2)
            {
                _graph.DrawLines(Pens.Blue, closeline.ToArray());
            }

            if (_tohlcv.Count > 0 && lim > 0.0001)
            {
                var last = _tohlcv[_tohlcv.Count - 1];

                if (closeprice < PriceHeight) if (_DrawWidth > Rule * 2 || Math.Abs(PriceHeight / 2 - closeprice) > 20)
                {
                    _graph.DrawString(last.Close.ToString(), SystemFonts.SmallCaptionFont,
                        Brushes.Black, _DrawWidth, closeprice);
                }
            }
        }
        
        private void DrawVolume(TOHLCV item, int x, double liv)
        {
            if (MaxVolume < 0.0001)
            {
                return;
            }

            var hvolume = (int)(item.Volume * liv);
            var rcvolume = new Rectangle(x, BmpHeight - hvolume, ItemWidth, hvolume);

            if (item.Open > item.Close && _clip.Contains(rcvolume))
            {
                _graph.FillRectangle(Brushes.Green, rcvolume);
            }
            else if (item.Open < item.Close && _clip.Contains(rcvolume))
            {
                _graph.FillRectangle(Brushes.Red, rcvolume);
            }
            else if (_clip.Contains(rcvolume))
            {
                _graph.FillRectangle(Brushes.Black, rcvolume);
            }
        }

        private int DrawPrice(TOHLCV item, int x, double lim, ref int cp)
        {
            var o = (int)((HighPrice - item.Open)*lim);
            var c = (int)((HighPrice - item.Close)*lim);
            var h = (int)((HighPrice - item.High)*lim);
            var l = (int)((HighPrice - item.Low)*lim);

            if (item.Open > item.Close)
            {
                var rc = new Rectangle(x, o, ItemWidth, c - o);
                if (_clip.Contains(rc)
                    && _clip.Contains(x + MidOffset, h) 
                    && _clip.Contains(x + MidOffset, l))
                {
                    cp = c;

                    _graph.FillRectangle(Brushes.Green, rc);
                    _graph.DrawLine(Pens.Green, x + MidOffset, h, x + MidOffset, l);
                }
            }
            else if (item.Open < item.Close)
            {
                var rc = new Rectangle(x, c, ItemWidth, o - c);
                if (_clip.Contains(rc) 
                    && _clip.Contains(x + MidOffset, h) 
                    && _clip.Contains(x + MidOffset, l))
                {
                    cp = c;

                    _graph.FillRectangle(Brushes.Red, rc);
                    _graph.DrawLine(Pens.Red, x + MidOffset, h, x + MidOffset, l);
                }
            }
            else
            {
                if (lim < 0.000001 && x + ItemWidth < _pad.Width)
                {
                    _graph.DrawLine(Pens.Black, 0, PriceHeight / 2, x + ItemWidth, PriceHeight / 2);
                }
                else if (item.Open > 0 && item.Close > 0
                    && _clip.Contains(x, o) && _clip.Contains(x+ItemWidth, o)
                    && _clip.Contains(x + MidOffset, h) && _clip.Contains(x + MidOffset, l))
                {
                    cp = o;

                    _graph.DrawLine(Pens.Black, x, o, x + ItemWidth, o);
                    _graph.DrawLine(Pens.Black, x + MidOffset, h, x + MidOffset, l);
                }
            }

            x += ItemWidth;
            item.RgnEnd = x;

            if (Regex.IsMatch(item.Time, ":30:")
                || Regex.IsMatch(item.Time, ":00:"))
            {
                _graph.DrawLine(Pens.LightGray, x, 0, x, PriceHeight);
                _graph.DrawString(item.Time, SystemFonts.SmallCaptionFont, Brushes.Black, x, 0);
                ++x;
            }

            return x;
        }
    }

    public class TOHLCV
    {
        public  int RgnEnd { get; set; }
        public string Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Time\t:{0}\nOpen\t:{1}\nHigh\t:{2}\nLow\t:{3}\nClose\t:{4}\nVolume\t:{5}",
                Time, Open, High, Low, Close, Volume);
        }

        private void bzero()
        {
            Open = Close = High = Low = Volume = 0;
        }

        public TOHLCV(string rec)
        {
            if (string.IsNullOrEmpty(rec))
            {
                throw new Exception("空行记录");
            }

            var spa = rec.Split(new[] { ',' }, StringSplitOptions.None);

            try
            {
                Time = spa[0];
                Open = Double.Parse(spa[1]);
                High = Double.Parse(spa[2]);
                Low = Double.Parse(spa[3]);
                Close = Double.Parse(spa[4]);
                Volume = double.Parse(spa[5]);
                
                if (double.IsNaN(Open) || double.IsInfinity(Open)
                    || double.IsNaN(High) || double.IsInfinity(High)
                    || double.IsNaN(Low) || double.IsInfinity(Low)
                    || double.IsNaN(Close) || double.IsInfinity(Close)
                    || Volume < 0 || Volume > 10000000 
                    || double.IsNaN(Volume) || double.IsInfinity(Volume))
                {
                    bzero();
                    return;
                }
            }
            catch (Exception)
            {
                bzero();
                //throw new Exception(rec + "：不正确的记录：" + ex.Message, ex);
            }
        }
    }
}
