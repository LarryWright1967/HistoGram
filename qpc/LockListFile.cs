using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qpc
{
    class LockListClass<T>
    {
        private List<T> tList;
        private object tLock = new object();
        //public LockListClass(LockListClass<T> inList)
        //{
        //    tList = new LockListClass<T>(inList);
        //}
        public LockListClass(List<T> inList)
        {
            tList = new List<T>(inList);
        }
        public LockListClass()
        {
            tList = new List<T>();
        }
        public void Add(T item)
        {
            lock (tLock)
            {
                tList.Add(item);
            }
        }
        public void AddRange(List<T> items)
        {
            tList.Add(items);
        }
        public int Count()
        {
            lock (tLock)
            {
                return tList.Count();
            }
        }
        public T GetItem(int index)
        {
            lock (tLock)
            {
                return tList[index];
            }
        }
        public List<T> Copy()
        {
            lock (tLock)
            {
                return new List<T>(tList);
            }
        }
        public void Clear()
        {
            lock (tLock)
            {
                tList.Clear();
            }
        }
        public List<T> SubSet(int startIndex, int count)
        {
            lock (tLock)
            {
                return new List<T>(tList.GetRange(startIndex, count));
            }
        }
        //private int NextIndex(int inVal) { int outVal = inVal++; if (outVal >= size) { outVal = 0; full = true; } return outVal; }
        //private int PreviousIndex(int inVal) { int outVal = inVal--; if (outVal < 0) { outVal = (size - 1); } return outVal; }
    }
}
