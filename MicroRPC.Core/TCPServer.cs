using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public interface IDataParse
    {
        void Parse(byte[] buffer, int length);
        event EventHandler<Object> PackageArrived;
    }

    public class SocketDataEventArgs : EventArgs
    {
        private Socket _workSocket;
        public Socket WorkSocket
        {
            get { return _workSocket; }
            set { _workSocket = value; }
        }

        private BufferPool _readBufferPool = new BufferPool(5, 100);

        private byte[] _buffer;
        /// <summary>
        /// this buffer should be not recycled after used by calling RecycleBuffer method
        /// </summary>
        public byte[] Buffer
        {
            get { return _buffer ?? (_buffer = _readBufferPool.Get()); }
            set { _buffer = value; }
        }

        private int _dataCount;
        public int DataCount
        {
            get { return _dataCount; }
            set { _dataCount = value; }
        }

        public IDataParse DataParse { get; set; }

        private Object _tag;
        //用来存储其他信息
        public Object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public SocketDataEventArgs()
        {
        }

        public void RecycleBuffer()
        {
            if (_buffer != null)
                _readBufferPool.Recycle(Buffer);
        }
    }
    
    public class TCPServer
    {
        private const int MTU = 1460;
        private Socket _listenSocket;

        public event EventHandler<SocketDataEventArgs> NewClientAccepted;
        public event EventHandler<Socket> ClientDisconnected;//client close the socket
        public event EventHandler ServerClosed;
        public event EventHandler<SocketDataEventArgs> DataReceived;
        public event EventHandler<int> DataSended;

        private int _maxConnection = 10000;
        private int _backlog = 1000;

        private ThreadSafeStackPool<Socket> _connectSocketPool;
        private ThreadSafeStackPool<SocketDataEventArgs> _socketDataArgsPool;
        private ThreadSafeStackPool<SocketAsyncEventArgs> _readSocketArgsPool;
        private ThreadSafeStackPool<SocketAsyncEventArgs> _writeSocketArgsPool;
        private Semaphore _connectionSemaphore;

        private int _port = 9006;
        public TCPServer()
        {
            _connectionSemaphore = new Semaphore(_maxConnection, _maxConnection);
        }
        public TCPServer(int port, int maxconnection)
        {
            _port = port;
            _maxConnection = maxconnection;
            _connectionSemaphore = new Semaphore(_maxConnection, _maxConnection);
        }

        private void InitPool()
        {
            _connectSocketPool = new ThreadSafeStackPool<Socket>(_maxConnection);
            _socketDataArgsPool = new ThreadSafeStackPool<SocketDataEventArgs>(_maxConnection);
            _readSocketArgsPool = new ThreadSafeStackPool<SocketAsyncEventArgs>(_maxConnection);
            _writeSocketArgsPool = new ThreadSafeStackPool<SocketAsyncEventArgs>(_maxConnection);
            for (int i = 0; i < _maxConnection; i++)
            {
                _connectSocketPool.Push(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
                _socketDataArgsPool.Push(new SocketDataEventArgs());

                var readSocketArgs = new SocketAsyncEventArgs();
                var buffer = new byte[MTU];
                readSocketArgs.SetBuffer(buffer, 0, buffer.Length);
                readSocketArgs.Completed += (s, e) => { ProcessEAPReceive(e); };
                _readSocketArgsPool.Push(readSocketArgs);

                var writeSocketArgs = new SocketAsyncEventArgs();
                writeSocketArgs.Completed += (s, e) => { ProcessEAPSend(e); };
                _writeSocketArgsPool.Push(writeSocketArgs);
            }
        }

        public bool Open()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
                if ((int)SocketOptionName.MaxConnections < _maxConnection)
                    Console.WriteLine("the max connnections is out of system requirement");
                _listenSocket.Listen(_backlog);
                InitPool();
                var args = new SocketAsyncEventArgs();
                args.Completed += (s, e) => { ProcessEAPAccept(e); };
                EAPStartAccept(args);
                return true;
            }
            catch (Exception ex)
            {
                Close();
                Console.WriteLine("Open TCP Server Error : \n" + ex.Message);
                return false;
            }
        }

        private void EAPStartAccept(SocketAsyncEventArgs e)
        {
            _connectionSemaphore.WaitOne();
            e.AcceptSocket = _connectSocketPool.Pop();
            bool r = false;
            try
            {
                r = _listenSocket.AcceptAsync(e);
                if (!r)
                    ProcessEAPAccept(e);
            }
            catch (ObjectDisposedException)//server closed
            {
                CloseSocket(e.AcceptSocket, true);
                //Open();//restart server or other handle ?
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void ProcessEAPAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {   //NewClientAccepted.BeginInvoke(this, socketDataArgs, null, null);// begininvoke will create a IAsyncResult object inside and damage performance, may use task or threadpool to inplace
                ThreadPool.QueueUserWorkItem(obj =>
                {
                    var socketDataArgs = _socketDataArgsPool.Pop();
                    socketDataArgs.WorkSocket = (Socket)obj;
                    var readArgs = _readSocketArgsPool.Pop();
                    readArgs.AcceptSocket = (Socket)obj;
                    readArgs.UserToken = socketDataArgs;
                    if (NewClientAccepted != null)
                    {
                        var temp = NewClientAccepted;
                        temp(this, socketDataArgs);
                    }
                    EAPStartReceive(readArgs);
                }, e.AcceptSocket);
            }
            else
            {
                CloseSocket(e.AcceptSocket, true);
            }
            EAPStartAccept(e);//continue to accept
        }

        private void EAPStartReceive(SocketAsyncEventArgs readArgs)
        {
            bool r = false;
            try
            {
                r = readArgs.AcceptSocket.ReceiveAsync(readArgs);
                if (!r)
                    ProcessEAPReceive(readArgs);
            }
            catch (ObjectDisposedException)//client closed
            {
                ReleaseReadArgs(readArgs);
            }
            catch (Exception ex)
            {
            }
        }

        private void ProcessEAPReceive(SocketAsyncEventArgs readArgs)
        {
            if (readArgs.SocketError == SocketError.Success && readArgs.BytesTransferred > 0)
            {
                if (DataReceived != null)
                {
                    try
                    {
                        var readDataArgs = (SocketDataEventArgs)readArgs.UserToken;
                        Buffer.BlockCopy(readArgs.Buffer, 0, readDataArgs.Buffer, 0, readArgs.BytesTransferred);
                        readDataArgs.DataCount = readArgs.BytesTransferred;
                        //DataReceived.BeginInvoke(this, readDataArgs, null, null);// begininvoke will create a IAsyncResult object inside and damage performance, may use task or threadpool to inplace
                        ThreadPool.QueueUserWorkItem(obj =>
                        {
                            var temp = DataReceived;
                            temp(this, (SocketDataEventArgs)obj);
                        }, readDataArgs);
                    }
                    catch
                    {
                        Console.WriteLine("TCPServer ProcessEAPReceive : handle the received raw data error");
                    }
                }
                EAPStartReceive(readArgs);//continue to receive
            }
            else
            {
                ReleaseReadArgs(readArgs);
            }
        }

        private void ReleaseReadArgs(SocketAsyncEventArgs readArgs)
        {
            var readDataArgs = (SocketDataEventArgs)readArgs.UserToken;
            readDataArgs.WorkSocket = null;
            readDataArgs.DataParse = null;
            _socketDataArgsPool.Push(readDataArgs);//recycle the SocketDataEventArgs to reuse
            CloseSocket(readArgs.AcceptSocket);

            readArgs.UserToken = null;
            _readSocketArgsPool.Push(readArgs);//recycle the SocketAsyncEventArgs to reuse
        }

        public void SendData(Socket workSocket, byte[] data)
        {
            var writeArgs = _writeSocketArgsPool.Pop();
            writeArgs.AcceptSocket = workSocket;
            writeArgs.SetBuffer(data, 0, data.Length);
            EAPSendData(writeArgs);
        }

        private void EAPSendData(SocketAsyncEventArgs writeArgs)
        {
            bool r = false;
            try
            {
                r = writeArgs.AcceptSocket.SendAsync(writeArgs);
                if (!r)
                    ProcessEAPSend(writeArgs);
            }
            catch (ObjectDisposedException)//client closed
            {
                CloseSocket(writeArgs.AcceptSocket);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private void ProcessEAPSend(SocketAsyncEventArgs writeArgs)
        {
            if (writeArgs.SocketError == SocketError.Success && writeArgs.BytesTransferred > 0)
            {
                if (DataSended != null)
                {   //DataSended.BeginInvoke(this, writeArgs.BytesTransferred, null, null);// begininvoke will create a IAsyncResult object inside and damage performance, may use task or threadpool to inplace
                    ThreadPool.QueueUserWorkItem(obj =>
                    {
                        var temp = DataSended;
                        temp(this, (int)obj);
                    }, writeArgs.BytesTransferred);
                }
            }
            else
            {
                CloseSocket(writeArgs.AcceptSocket);
            }
            _writeSocketArgsPool.Push(writeArgs);//recycle the write SocketAsyncArgs
        }

        private void CloseSocket(Socket workSocket, bool serverSide = false)
        {
            if (workSocket != null)
            {
                if (ClientDisconnected != null)
                {
                    var temp = ClientDisconnected;
                    temp(this, workSocket);//sync handle
                }
                try
                {
                    if (serverSide && workSocket.Connected || !serverSide)
                    {
                        _connectSocketPool.Push(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
                        _connectionSemaphore.Release();
                    }
                    if (workSocket.Connected) //server side request close
                        workSocket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                workSocket.Close();
                workSocket = null;
            }
        }

        public void Close()
        {
            if (_listenSocket != null)
            {
                try
                {
                    _listenSocket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                _listenSocket.Close();
                _listenSocket = null;
                if (ServerClosed != null)
                {
                    var temp = ServerClosed;
                    temp(this, EventArgs.Empty);
                }
            }
        }

    }
}
