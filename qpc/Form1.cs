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
            Build();
            displayTimer = new System.Timers.Timer();
            displayTimer.Interval = 100;
            displayTimer.Elapsed += Form1_Elapsed; ;
            displayTimer.Start();
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
                Task.Run(() => { while (run) { AddData(new List<ulong> { GenRand() }); } });
            }
        }

        private void Form1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<ulong> ld = GetData();
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
        private List<ulong> GetData() { lock (locker) { return new List<ulong>(rNumList); } }
        private void ClearData() { lock (locker) { rNumList.Clear(); } }
        private void AddData(List<ulong> dataToAdd) { lock (locker) { rNumList.AddRange(dataToAdd); } }
        private ulong GenRand()
        {
            ulong dat = 0;
            for (int i = 0; i < 64; i++)
            {
                System.Threading.Thread.Sleep(1);
                QueryPerformanceCounter(out long t);
                ulong b = (ulong)(t & 1);
                dat = dat << 1;
                dat = dat | b;
            }
            return dat;
        }
        private int[] GenHistoData(List<ulong> ldata)
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
