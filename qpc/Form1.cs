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
using System.Numerics;

namespace qpc
{
    public partial class Form1 : Form
    {
        private System.Timers.Timer displayTimer = new System.Timers.Timer();

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        private LockListClass<decimal> rNumList = new LockListClass<decimal>();
        private object locker = new object();
        private int bucketPower;
        private uint buckets;
        private Stopwatch sw = new Stopwatch();
        private bool run = true;

        private decimal highRandLimit, offset;
        private ulong multiplyer, count;
        private int randBits;

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
            //Build();
            displayTimer = new System.Timers.Timer();
            displayTimer.Interval = 500;
            displayTimer.Elapsed += Form1_Elapsed; ;
            displayTimer.Start();
        }
        private void Button3_Click(object sender, EventArgs e)
        {// get size, offset and scale. return value number in the range unscaled and unoffseted?
            if (!decimal.TryParse(textBox1.Text, out decimal input1)) { throw new ArgumentException($"The text {textBox1.Text} can not be converted to a decimal value."); }
            if (!decimal.TryParse(textBox2.Text, out decimal input2)) { throw new ArgumentException($"The text {textBox2.Text} can not be converted to a decimal value."); }
            decimal minDec = Math.Min(input1, input2);
            decimal maxDec = Math.Max(input1, input2);
            offset = minDec; // to use to offset value back to the desired range.
            multiplyer = Math.Max(GetScaleFromDec(minDec), GetScaleFromDec(maxDec)); // used to shift all the values into the integer range
            highRandLimit = (maxDec - minDec) * multiplyer; // the minimum value to compare to the random value to eliminate values outside of the desired range.
            randBits = MinBiDigits(highRandLimit); // calculate the size of the binary number to generate with the random number generator.

            // calculate random number
            // remove if value is over highRandLimit
            // divide by multiplyer
            // add multiplyer

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
            for (int i = 0; i < 1; i++)
            {
                Task.Run(() =>
                {
                    while (run)
                    {
                        AddData(GenRand(randBits));
                    }
                });
            }
        }
        #region graph
        List<decimal> displayList = new List<decimal>();
        int[] ba;
        private void Form1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (locker)
            {
                List<decimal> displayList = rNumList.Copy(); // GetData();
                ba = GenHistoData(displayList);
            }
            if (displayList.Count() > 0)
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
                        label8.Text = displayList.Min().ToString();
                        label9.Text = displayList.Max().ToString();
                        label4.Text = displayList.Count.ToString();
                            //label1.Text = time.ToString();
                        });
                }
            }
        }
        #region histo
        int[] bitbuckets = new int[1024];
        private int[] GenHistoData(List<decimal> ldata)
        {
            if (ldata.Count > 1024) { }
            //bitbuckets = new int[buckets];
            foreach (ulong l in ldata)
            {
                ulong bucketSize = ulong.MaxValue / (buckets - 1);
                int index = (int)(l / bucketSize);
                //if (bitbuckets[index] > 1) Debug.Print($"val = {Convert.ToString(l, toBase: 2), 32}"); 
                bitbuckets[index]++;
            }
            return bitbuckets;
        }
        #endregion
        #endregion
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
        private List<decimal> GetData() {  return rNumList.Copy(); }
        private void ClearData() { lock (locker) { rNumList.Clear(); } }
        private void AddData(decimal dataToAdd) { lock (locker) { rNumList.Add(dataToAdd); } }
        private void AddData(List<decimal> dataToAdd) { lock (locker) { rNumList.AddRange(dataToAdd); } }
        private ulong GetScaleFromDec(decimal dVal)
        {
            // https://stackoverflow.com/questions/13477689/find-number-of-decimal-places-in-decimal-value-regardless-of-culture
            // https://stackoverflow.com/users/1477076/burning-legion
            return (ulong)Math.Pow(10, BitConverter.GetBytes(Decimal.GetBits(dVal)[3])[2]);
        }
        /// <summary>Returns the minimum amount of binary digits needed to represent the value</summary>
        private int MinBiDigits(decimal dVal)
        {
            int digits = 1;
            while (CreateMaxDecimalFromBitCount(digits) < dVal) { digits++; }
            return digits;
        }

        //private int RequiredBits(decimal HiDec, decimal LowDec, byte maxScale)
        //{ //https://docs.microsoft.com/en-us/dotnet/api/system.math.log?view=netframework-4.8#System_Math_Log_System_Double_System_Double_
        //    decimal hd = HiDec * maxScale;
        //    decimal ld = LowDec * maxScale;
        //    decimal dif = hd - ld;
        //    return (int)(Math.Log((double)dif, 2) + 1);
        //}
        //private decimal ScaleFromScaleFactor(int scale)
        //{
        //    return (ulong)Math.Pow(10, scale);
        //}
        //private byte GetScale(decimal dVal)
        //{
        //    return BitConverter.GetBytes(Decimal.GetBits(dVal)[3])[2];
        //}
        //private ulong Dmax(ulong d1, ulong d2)
        //{
        //    return Math.Max(d1, d2);
        //}
        //private byte MaxScaleFactorFromDecimals(decimal HiDec, decimal LowDec)
        //{
        //    return Math.Max(GetScaleFactorFromDecimal(HiDec), GetScaleFactorFromDecimal(LowDec));
        //}
        //private byte GetScaleFactorFromDecimal(decimal dVal)
        //{
        //    return BitConverter.GetBytes(Decimal.GetBits(dVal)[3])[2];
        //}

        #region genrand
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
            byte scaleFactor = 0;
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

            return new decimal(lo: lo, mid: mid, hi: hi, isNegative: sign, scale: scaleFactor);
        }
        private int RandInt(int bits)
        {
            if (bits < 1) { return 0; }
            if (bits > 32) { throw new ArgumentException($"The number of bits, {bits} can not be converted to a int value."); }
            int dat = 0;
            for (int i = 0; i < bits; i++) // bit indexes 0 - 31
            {
                System.Threading.Thread.Sleep(1);
                QueryPerformanceCounter(out long t);
                int b = (int)(t & 1);
                dat = dat << 1;
                dat = dat | b;
            }
            return dat;
        }
        #endregion


        #region create max decimal for x bits
        private decimal CreateMaxDecimalFromBitCount(int bits)
        {
            if (bits > 95) { throw new ArgumentException($"The number of bits, {bits} can not be converted to a decimal value."); }
            int lo = 0;
            int mid = 0;
            int hi = 0;
            bool sign = false;
            byte scaleFactor = 0;
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
            return new decimal(lo: lo, mid: mid, hi: hi, isNegative: sign, scale: scaleFactor);
        }
        private int CreateMaxIntFromBitCount(int bits)
        {
            if (bits < 1) { return 0; }
            int returnValue = 1;
            if (bits > 1) { for (int i = 0; i < bits - 1; i++) { returnValue = returnValue << 1; returnValue = returnValue | 1; } }
            return returnValue;
        }
        #endregion

        #region set
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
        #endregion
    }
}
