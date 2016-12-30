using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    /// <summary>
    /// buffer pool for tcp data buffer
    /// </summary>
    class BufferPool
    {
        private const int BUFFER_SIZE = 1460;
        private int _bufferSize = BUFFER_SIZE;
        private int _count;
        private Stack<byte[]> _pool = new Stack<byte[]>();
        private object _lockObj = new object();
        public BufferPool(int buffersize = BUFFER_SIZE)
        {
            _count = Environment.ProcessorCount * 2;
            _bufferSize = buffersize;
            InitPool(_count, _bufferSize);
        }
        public BufferPool(int count, int buffersize = BUFFER_SIZE)
        {
            _count = count;
            _bufferSize = buffersize;
            InitPool(_count, _bufferSize);
        }

        private void InitPool(int count, int buffersize)
        {
            if (count <= 0 || buffersize <= 0) return;
            _pool = new Stack<byte[]>(count);
            for (int i = 0; i < count; i++)
                _pool.Push(new byte[_bufferSize]);
        }

        public byte[] Get()
        {
            lock (_lockObj)
            {
                if (_pool.Count == 0)
                    return new byte[_bufferSize];
                return _pool.Pop();
            }
        }

        public bool Recycle(byte[] buffer)
        {
            lock (_lockObj)
            {
                if (buffer == null || buffer.Length != _bufferSize) return false;
                _pool.Push(buffer);
                return true;
            }
        }

        public void Clean()
        {
            lock (_lockObj)
            {
                _pool.Clear();
            }
        }

    }
}
