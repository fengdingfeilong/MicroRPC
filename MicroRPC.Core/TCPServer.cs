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
    public class SocketDataEventArgs : EventArgs
    {
        private Socket _workSocket;
        public Socket WorkSocket
        {
            get { return _workSocket; }
            set { _workSocket = value; }
        }

        private byte[] _data;
        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
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
        { }
    }

    public class TCPServer
    {
        private Socket listenSocket;

        public event EventHandler<Socket> NewClientAccepted;
        public event EventHandler<Socket> ClientDisconnected;//client close the socket
        public event EventHandler ServerClosed;
        public event EventHandler<SocketDataEventArgs> DataReceived;
        public event EventHandler<int> DataSended;

        private int _maxConnection = 10000;

        private BufferPool _bufferPool = new BufferPool(10000);

        private int _port = 9006;
        public TCPServer()
        {
        }
        public TCPServer(int port, int maxconnection)
        {
            _port = port;
            _maxConnection = maxconnection;
        }

        /// <summary>
        /// 是否使用异步方式通信
        /// </summary>
        public bool UseAsyncMode = false;

        public void Open()
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
                if ((int)SocketOptionName.MaxConnections < _maxConnection)
                    Console.WriteLine("the max connnections is out of system requirement");
                listenSocket.Listen(_maxConnection);
                if (UseAsyncMode)
                    listenSocket.BeginAccept(new AsyncCallback(HandleAccept), null);  //使用异步方式              
                else
                {
                    Task.Factory.StartNew(() =>
                    {
                        SyncAccept();
                    });
                }
            }
            catch
            {
                Close();
            }
        }

        //经测试，使用同步方式效果比异步的好
        private void SyncAccept()
        {
            while (true)
            {
                Socket socket = null;
                try
                {
                    socket = listenSocket.Accept();
                }
                catch (ObjectDisposedException ex)//server close the socket
                {
                }
                catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
                {
                    CloseSocket(socket);
                }
                catch (Exception ex)
                {
                    throw;
                }
                if (socket == null)
                {
                    Console.WriteLine("accept failed");
                    Thread.Sleep(500);
                    continue;
                }
                if (socket != null && NewClientAccepted != null) NewClientAccepted(this, socket);
                Task.Factory.StartNew(w =>
                {
                    Socket workSocket = (Socket)w;
                    while (true)
                    {
                        var buffer = new byte[1460];
                        int count = 0;
                        try
                        {
                            count = workSocket.Receive(buffer);
                        }
                        catch (ObjectDisposedException ex)//server close the socket
                        {
                        }
                        catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
                        {
                            CloseSocket(workSocket);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                        SocketDataEventArgs dataargs = new SocketDataEventArgs();
                        dataargs.WorkSocket = workSocket;
                        dataargs.Data = new byte[count];
                        Array.Copy(buffer, dataargs.Data, count);
                        if (DataReceived != null && count > 0)
                            DataReceived(this, dataargs);
                    }
                }, socket);
            }
        }

        public void SendData(SocketDataEventArgs args)
        {
            if (args == null || args.WorkSocket == null || !args.WorkSocket.Connected || args.Data == null) return;
            try
            {
                Task.Factory.StartNew(() =>
                {
                    args.WorkSocket.Send(args.Data);
                });
            }
            catch (ObjectDisposedException ex)//server close the socket
            {
            }
            catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
            {
                CloseSocket(args.WorkSocket);
            }
            catch
            {
                throw;
            }
        }

        private void HandleAccept(IAsyncResult result)
        {
            Socket workSocket = null;
            try
            {
                if (listenSocket != null)
                {
                    lock (listenSocket)
                    {
                        if (result.CompletedSynchronously)
                            workSocket = listenSocket.Accept();
                        else
                            workSocket = listenSocket.EndAccept(result);
                        BeginRecieve(workSocket);
                        listenSocket.BeginAccept(new AsyncCallback(HandleAccept), null);
                    }
                }
            }
            catch (ObjectDisposedException ex)//server close the socket
            {
            }
            catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
            {
                CloseSocket(workSocket);
            }
            catch (Exception ex)
            {
                throw;
            }
            if (workSocket != null && NewClientAccepted != null) NewClientAccepted(this, workSocket);
        }

        private void BeginRecieve(Socket workSocket)
        {
            if (workSocket != null && workSocket.Connected)
            {
                byte[] buffer = _bufferPool.Get();
                SocketDataEventArgs dataargs = new SocketDataEventArgs();
                dataargs.WorkSocket = workSocket;
                dataargs.Data = buffer;
                dataargs.WorkSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(HandleReceive), dataargs);
            }
        }

        private void HandleReceive(IAsyncResult result)
        {
            if (result == null || result.AsyncState == null) return;
            SocketDataEventArgs dataargs = (SocketDataEventArgs)result.AsyncState;
            Socket workSocket = dataargs.WorkSocket;
            byte[] buffer = dataargs.Data;
            int count = 0;
            try
            {
                if (result.CompletedSynchronously)
                    count = workSocket.Receive(buffer);
                else
                    count = workSocket.EndReceive(result);
                if (count > 0)
                {
                    byte[] data = new byte[count];
                    Array.Copy(buffer, data, count);
                    dataargs.Data = data;
                }
                _bufferPool.Recycle(buffer);
                BeginRecieve(workSocket);
            }
            catch (ObjectDisposedException ex)//server close the socket
            {
            }
            catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
            {
                CloseSocket(workSocket);
            }
            catch (Exception ex)
            {
                throw;
            }
            if (DataReceived != null && count > 0) DataReceived(this, dataargs);
        }

        public void SendDataAsync(SocketDataEventArgs args)
        {
            if (args == null || args.WorkSocket == null || !args.WorkSocket.Connected || args.Data == null) return;
            try
            {
                args.WorkSocket.BeginSend(args.Data, 0, args.Data.Length, SocketFlags.None, new AsyncCallback(HandleSend), args);
            }
            catch (ObjectDisposedException ex)//server close the socket
            {
            }
            catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
            {
                CloseSocket(args.WorkSocket);
            }
            catch
            {
                throw;
            }
        }

        private void HandleSend(IAsyncResult result)
        {
            if (result == null || result.AsyncState == null) return;
            SocketDataEventArgs dataargs = (SocketDataEventArgs)result.AsyncState;
            Socket workSocket = dataargs.WorkSocket;
            byte[] buffer = dataargs.Data;
            int count = 0;
            try
            {
                if (result.CompletedSynchronously)
                    count = workSocket.Send(buffer);
                else
                    count = workSocket.EndSend(result);
                BeginRecieve(workSocket);
            }
            catch (ObjectDisposedException ex)//server close the socket
            {
            }
            catch (SocketException ex) //client close the socket ( SocketError.ConnectionReset ) 
            {
                CloseSocket(workSocket);
            }
            catch
            {
                throw;
            }
            if (DataSended != null) DataSended(this, count);
        }

        private void CloseSocket(Socket socket)
        {
            if (socket != null)
            {
                if (ClientDisconnected != null && socket.Connected) ClientDisconnected(this, socket);
                try
                {
                    if (socket.Connected)
                        socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                socket.Close();
                socket = null;
            }
        }

        public void Close()
        {
            if (listenSocket != null)
            {
                try
                {
                    listenSocket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                listenSocket.Close();
                listenSocket = null;
                if (ServerClosed != null) ServerClosed(this, EventArgs.Empty);
            }
        }

    }
}
