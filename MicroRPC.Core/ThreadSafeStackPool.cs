using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public class ThreadSafeStackPool<T>
    {
        private Stack<T> m_pool;
        public ThreadSafeStackPool(int capacity)
        {
            m_pool = new Stack<T>(capacity);
        }
        public virtual void Push(T item)
        {
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }
        public virtual void Recycle(T item)
        {
            Push(item);
        }
        public virtual T Pop()
        {
            lock (m_pool)
            {
                if (m_pool.Count > 0)
                    return m_pool.Pop();
                else return default(T);
            }
        }
        public int Count
        {
            get
            {
                lock (m_pool)
                {
                    return m_pool.Count;
                }
            }
        }
    }
}
