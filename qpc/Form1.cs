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

        private Stopwatch sw = new Stopwatch();
        private bool run = false;

        private decimal highRandLimit;
        private int count;
        int bucketCount;
        int[] bitbuckets;

        private RandStruct RandStruct1;

        public Form1()
        {
            this.Shown += Form1_Shown;
            InitializeComponent();
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            sw.Start();
            //chart1.ChartAreas[0].AxisX.Maximum = 255;
            //chart1.ChartAreas[0].AxisX.Minimum = 0;
            //chart1.ChartAreas[0].AxisX.MaximumAutoSize = 100;
            button1.Click += Button1_Click;
            button2.Click += Button2_Click;
            button3.Click += Button3_Click;
            //Build();
            displayTimer = new System.Timers.Timer();
            displayTimer.Interval = 2000;
            displayTimer.Elapsed += Form1_Elapsed; ;
            displayTimer.Start();
        }
        private void Button3_Click(object sender, EventArgs e)
        {// get size, offset and scale. return value number in the range unscaled and unoffseted?
            if (!decimal.TryParse(textBox1.Text, out decimal input1)) { throw new ArgumentException($"The text {textBox1.Text} can not be converted to a decimal value."); }
            if (!decimal.TryParse(textBox2.Text, out decimal input2)) { throw new ArgumentException($"The text {textBox2.Text} can not be converted to a decimal value."); }
            RandStruct1 = new RandStruct(input1, input2);
            if (!int.TryParse(textBox3.Text, out int countInput)) { throw new ArgumentException($"The text {textBox3.Text} can not be converted to a integer value."); }
            count = countInput;
            //if (buckets > 999) { buckets = 999; }
            //bitbuckets = new int[(int)buckets];

            // clear data
            ClearData();
            // calculate random number
            Build();

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
            if (!run)
            {
                run = true;
                for (int i = 0; i < 100; i++) // One instance
                {
                    Task.Run(() =>
                    {
                        while (run & rNumList.Count() < count)
                        {
                            decimal d = RandStruct1.GetRand();
                            if (d <= (RandStruct1.valueRange * RandStruct1.multiplyer) && d >= 0m)
                            {
                                //decimal d2 = (d / multiplyer) + offset;
                                AddData(d);
                            }
                        }
                    });
                }
            }
        }

        #region graph
        List<decimal> displayList = new List<decimal>();
        int[] ba;
        private void Form1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<decimal> displayList = rNumList.Copy();
            ba = GenHistoData(displayList);
            if (displayList.Count() > 0)
            {
                try
                {
                    Set(chart1, () =>
                    {
                        chart1.Series[0].Points.Clear();
                        for (int i = 0; i < bucketCount; i++)
                        {
                            //double bucketSize = (double)(range / (bitbuckets.Length - 1));
                            //decimal xVal = (int)(((ulong)i / multiplyer) / bucketSize); // (decimal)i / (decimal)multiplyer + offset;
                            if ((int)(i / 50) == i / 50.0)
                            {
                                int kkls = 12;
                            }
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
        private int[] GenHistoData(List<decimal> ldata)
        {
            bucketCount = (int)(RandStruct1.valueRange * RandStruct1.multiplyer);
            bitbuckets = new int[bucketCount+1];
            foreach (decimal d in rNumList.Copy())
            {
                bitbuckets[(int)(d * RandStruct1.multiplyer)]++;
            }
            return bitbuckets;
        }
        #endregion
        #endregion

        private void Button1_Click(object sender, EventArgs e)
        {
            run = false;
            Task.WaitAll();
            System.Threading.Thread.Sleep(250);
            Task.Run(() =>
            {
                ClearData();
            });
            Task.WaitAll();
        }

        // https://stackoverflow.com/questions/13447248/c-sharp-how-to-assign-listt-without-being-a-reference
        // https://stackoverflow.com/users/2982/inisheer
        private List<decimal> GetData() { return rNumList.Copy(); }
        private void ClearData() { rNumList.Clear(); }
        private void AddData(decimal dataToAdd) { rNumList.Add(dataToAdd); }

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
