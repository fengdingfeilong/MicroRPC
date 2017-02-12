using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    /// <summary>
    /// buffer pool for tcp data buffer, this pool can dynamicly adjust the capacity between minCapacity and maxCapacity
    /// </summary>
    class BufferPool
    {
        private const int BUFFER_SIZE = 1460;
        private int _bufferSize = BUFFER_SIZE;
        private int _minCapacity = 1;
        private int _maxCapacity = 1000;
        private int _capacity;
        private Stack<byte[]> _pool = new Stack<byte[]>();
        private object _lockObj = new object();
        public BufferPool(int buffersize = BUFFER_SIZE)
        {
            _bufferSize = buffersize;
            _capacity = _minCapacity;
            InitPool(_capacity, _bufferSize);
        }
        public BufferPool(int minCapacity = 1, int maxCapacity = 1000, int buffersize = BUFFER_SIZE)
        {
            _minCapacity = minCapacity;
            _maxCapacity = maxCapacity;
            _capacity = _minCapacity;
            _bufferSize = buffersize;
            InitPool(_capacity, _bufferSize);
        }

        private void InitPool(int capacity, int buffersize)
        {
            if (capacity <= 0 || buffersize <= 0)
                throw new Exception("capacity or buffersize must bigger than zero");
            _pool = new Stack<byte[]>();
            for (int i = 0; i < capacity; i++)
                _pool.Push(new byte[_bufferSize]);
        }

        public byte[] Get()
        {
            lock (_lockObj)
            {
                if (_pool.Count < _minCapacity)
                {
                    _capacity *= 2;
                    if (_capacity > _maxCapacity) _capacity = _maxCapacity;
                    for (int i = 0; i < _capacity - _pool.Count; i++)
                        _pool.Push(new byte[_bufferSize]);
                }
                return _pool.Pop();
            }
        }

        /// <summary>
        /// if do not recycle, the capacity will keep at maxCapacity, and this will damage performance
        /// </summary>
        /// <param name="buffer"></param>
        public void Recycle(byte[] buffer)
        {
            lock (_lockObj)
            {
                if (buffer == null || buffer.Length != _bufferSize)
                    throw new Exception("the buffer size is not match to the pool");
                _pool.Push(buffer);
                if (_capacity > _minCapacity)
                    _capacity--;//if buffer recycled, turn down the capacity
            }
        }

    }
}
