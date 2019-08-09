using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace qpc
{
    public partial class Form1 : Form
    {
        //private System.Timers.Timer[] ts = new System.Timers.Timer[1000];
        private System.Timers.Timer displayTimer = new System.Timers.Timer();

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        //long time;
        List<decimal> rNumList = new List<decimal>();
        object locker = new object();
        int bucketPower;
        uint buckets;
        Stopwatch sw = new Stopwatch();
        bool run = true;
        decimal min, max;

        public Form1()
        {
            this.Shown += Form1_Shown;
            bucketPower = 16;
            buckets = (uint)1 << bucketPower;
            InitializeComponent();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            sw.Start();
            //chart1.ChartAreas[0].AxisX.Maximum = 255;
            //chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.MaximumAutoSize = 100;
            button1.Click += Button1_Click;
            button2.Click += Button2_Click;
            button3.Click += Button3_Click;
            Build();
            displayTimer = new System.Timers.Timer();
            displayTimer.Interval = 100;
            displayTimer.Elapsed += Form1_Elapsed; ;
            displayTimer.Start();
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            if (decimal.TryParse(textBox1.Text, out decimal d1)) { min = d1; } else { throw new ArgumentException($"The text {textBox1.Text} can not be converted to a decimal value."); }
            if (decimal.TryParse(textBox2.Text, out decimal d2)) { max = d2; } else { throw new ArgumentException($"The text {textBox2.Text} can not be converted to a decimal value."); }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            if (run)
            {
                run = false;
            }
            else
            {
                Build();
            }

        }

        private void Build()
        {
            run = true;
            for (int i = 0; i < 50; i++)
            {
                Task.Run(() => { while (run) { AddData(new List<decimal> { GenRand(32) }); } });
            }
        }

        private void Form1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<decimal> ld = GetData();
            int[] ba = GenHistoData(ld);
            if (ld.Count() > 0)
            {
                try
                {
                    Set(chart1, () =>
                    {
                        chart1.Series[0].Points.Clear();
                        for (int i = 0; i < buckets; i++)
                        {
                            chart1.Series[0].Points.AddXY(i, ba[i]);
                        }
                    });
                }
                finally
                {
                    Set(chart1, () =>
                    {
                        chart1.Update();
                        label8.Text = ld.Min().ToString();
                        label9.Text = ld.Max().ToString();
                        label4.Text = ld.Count.ToString();
                        //label1.Text = time.ToString();
                    });
                }
            }
        }

        private void StartTimers()
        {
            //foreach (System.Timers.Timer t in ts)
            //{
            //    t.Start();
            //}
        }
        private void StopTimers()
        {
            //foreach (System.Timers.Timer t in ts)
            //{
            //    t.Start();
            //}
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            //StopTimers();
            run = false;
            Task.WaitAll();
            System.Threading.Thread.Sleep(250);
            Task.Run(() =>
            {
                ClearData();
                //for (int i = 0; i < 1024; i++) // foreach bitbucket
                //{
                //    //bitbuckets[i] = 0;
                //    //count1 = 0;
                //    //count2 = 0;
                //    //num = 0;
                //}
            });
            Task.WaitAll();
            Build();
            //StartTimers();
        }
        // https://stackoverflow.com/questions/13447248/c-sharp-how-to-assign-listt-without-being-a-reference
        // https://stackoverflow.com/users/2982/inisheer
        private List<decimal> GetData() { lock (locker) { return new List<decimal>(rNumList); } }
        private void ClearData() { lock (locker) { rNumList.Clear(); } }
        private void AddData(List<decimal> dataToAdd) { lock (locker) { rNumList.AddRange(dataToAdd); } }
        private int RequiredBits(decimal HiDec, decimal LowDec, byte maxScale)
        { //https://docs.microsoft.com/en-us/dotnet/api/system.math.log?view=netframework-4.8#System_Math_Log_System_Double_System_Double_
            decimal hd = HiDec * maxScale;
            decimal ld = LowDec * maxScale;
            decimal dif = hd - ld;
            return (int)(Math.Log((double)dif, 2) + 1);
        }
        private byte MaxScale(decimal HiDec, decimal LowDec)
        {
            // https://stackoverflow.com/questions/13477689/find-number-of-decimal-places-in-decimal-value-regardless-of-culture
            // https://stackoverflow.com/users/1477076/burning-legion
            byte scale1 = BitConverter.GetBytes(decimal.GetBits(HiDec)[3])[2];
            byte scale2 = BitConverter.GetBytes(decimal.GetBits(LowDec)[3])[2];
            return Math.Max(scale1, scale2);
        }
        private decimal CreateMaxDecimalFromBitCount(int bits)
        {
            if (bits > 95) { throw new ArgumentException($"The number of bits, {bits} can not be converted to a decimal value."); }
            int lo = 0;
            int mid = 0;
            int hi = 0;
            bool sign = false;
            byte scale = 0;
            if (bits > 64)
            { // 3 ints 2 ints full, 1 partial
                lo = CreateMaxIntFromBitCount(32);
                mid = CreateMaxIntFromBitCount(32);
                hi = CreateMaxIntFromBitCount(bits - 64);
            }
            else if (bits > 32)
            {// 2 ints 1 ints full, 1 partial
                lo = CreateMaxIntFromBitCount(32);
                mid = CreateMaxIntFromBitCount(bits - 32);
                hi = CreateMaxIntFromBitCount(0);
            }
            else if (bits > 0)
            { // 1 int, 1 partial
                lo = CreateMaxIntFromBitCount(bits);
                mid = CreateMaxIntFromBitCount(0);
                hi = CreateMaxIntFromBitCount(0);
            }
            else { throw new ArgumentException($"The number of bits, {bits} can not be converted to a decimal value."); }
            return new decimal(lo: lo, mid: mid, hi: hi, isNegative: sign, scale: scale);
        }
        private int CreateMaxIntFromBitCount(int bits)
        {
            if (bits < 1) { throw new ArgumentException($"The number of bits, {bits} can not be converted to a int value."); }
            int returnValue = 1;
            if (bits > 1) { for (int i = 0; i < bits - 1; i++) { returnValue = returnValue << 1; returnValue = returnValue | 1; } }
            return returnValue;
        }
        private decimal GenRand(int bits)
        {

            //// decimal places
            //decimal dVal = 456.789M;
            //int[] parts = Decimal.GetBits(dVal);
            //int lo = parts[0];
            //int mid = parts[1];
            //int hi = parts[2];
            //bool sign = (parts[3] & 0x80000000) != 0;
            //byte scale = (byte)((parts[3] >> 16) & 0x7F);
            //scale = BitConverter.GetBytes(decimal.GetBits(dVal)[3])[2];
            //decimal d = new decimal(lo: lo, mid: mid, hi: hi, isNegative: sign, scale: scale);

            if (bits > 95) { throw new ArgumentException($"The number of bits, {bits} can not be converted to a decimal value."); }
            int lo = 0;
            int mid = 0;
            int hi = 0;
            bool sign = false;
            byte scale = 0;
            if (bits > 64)
            { // 3 ints 2 ints full, 1 partial
                lo = RandInt(32);
                mid = RandInt(32);
                hi = RandInt(bits - 64);
            }
            else if (bits > 32)
            {// 2 ints 1 ints full, 1 partial
                lo = RandInt(32);
                mid = RandInt(bits - 32);
                hi = RandInt(0);
            }
            else if (bits > 0)
            { // 1 int, 1 partial
                lo = RandInt(bits);
                mid = RandInt(0);
                hi = RandInt(0);
            }
            else { throw new ArgumentException($"The number of bits, {bits} can not be converted to a decimal value."); }

            return new decimal(lo: lo, mid: mid, hi: hi, isNegative: sign, scale: scale);
        }
        private int RandInt(int bits)
        {
            if (bits > 32) { throw new ArgumentException($"The number of bits, {bits} can not be converted to a int value."); }
            int dat = 0;
            for (int i = 0; i < bits; i++)
            {
                System.Threading.Thread.Sleep(1);
                QueryPerformanceCounter(out long t);
                int b = (int)(t & 1);
                dat = dat << 1;
                dat = dat | b;
            }
            return dat;
        }
        private int[] GenHistoData(List<decimal> ldata)
        {
            int[] bitbuckets = new int[buckets];
            //foreach (ulong l in ldata)
            //{
            //    ulong bucketSize = ulong.MaxValue / (buckets - 1);
            //    int index = (int)(l / bucketSize);
            //    //if (bitbuckets[index] > 1) Debug.Print($"val = {Convert.ToString(l, toBase: 2), 32}"); 
            //    bitbuckets[index]++;
            //}
            return bitbuckets;
        }


        public void Set(Control c, Action a)
        {
            if (c != null && !c.IsDisposed && c.IsHandleCreated)
            {
                if (c.InvokeRequired)
                {
                    c.BeginInvoke(a);
                }
                else
                {
                    a();
                }
            }
        }
    }
}
