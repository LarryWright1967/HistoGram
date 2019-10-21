using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace GenRandDouble
{
    /* 
     * shift
     * multiply
     */

    public struct RandStruct
    {
        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        public double MinVal { get; set; }
        public double MaxVal { get; set; }
        public double Offset { get; set; }
        public int Multiplyer { get; set; }
        public int MinBits { get; set; }
        public double ValueRange { get; set; }
        double val1, val2, highVal, LowVal;
        public RandStruct(double val1, double val2)
        {
            this.val1 = val1;
            this.val2 = val2;
            MaxVal = Math.Max(this.val1, this.val2);
            MinVal = Math.Min(this.val1, this.val2);
            ValueRange = MaxVal - MinVal;
            //testResult = 0m;
            Offset = MinVal;
            Multiplyer = 0;
            MinBits = 0;
            highVal = 0.0;
            LowVal = 0.0;
            highVal = GetMultiplyer(MaxVal);
            LowVal = GetMultiplyer(MinVal);
            Multiplyer = (int)Math.Max(LowVal, highVal);
            MinBits = MinBiDigits((long)(ValueRange * (double)Multiplyer)); // calculate the size of the binary number to generate with the random number generator.
        }

        private double GetMultiplyer(double dVal)
        {
            // https://stackoverflow.com/questions/13477689/find-number-of-decimal-places-in-decimal-value-regardless-of-culture
            // https://stackoverflow.com/users/1477076/burning-legion
            return (double)Math.Pow(10, dVal);
        }

        public double GetRand()
        {
            double testResult;
            do
            {
                // binary values do not necessaryly match
                // the same value range as the desired range.
                // this function allows to discard values that
                // are greater then the desired range.
                do
                {
                    testResult = (double)GenRand(MinBits);
                } while (testResult > (ValueRange * Multiplyer));
                if (Multiplyer == 0)
                {
                    testResult += Offset;
                }
                else
                {
                    testResult = (testResult / Multiplyer) + Offset;
                }
            } while (testResult < MinVal || testResult > (MaxVal - 0));
            return testResult;
        }

        /// <summary>Returns the minimum amount of binary digits needed to represent the value</summary>
        private int MinBiDigits(long lVal)
        {
            int digits = 1;
            while (CreateMaxDecimalFromBitCount(digits) < lVal) { digits++; }
            return digits;
        }

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

            decimal d = new decimal(lo: lo, mid: mid, hi: hi, isNegative: sign, scale: scaleFactor);
            return d;
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

    }
}
