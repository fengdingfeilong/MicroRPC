using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public class ClientSocketManager
    {
        public ClientSocketManager()
        {
        }

        private Object m_lock = new Object();
        private List<TCPClientInfo> _clients;
        public List<TCPClientInfo> Clients
        {
            get { return _clients ?? (_clients = new List<TCPClientInfo>()); }
            set { _clients = value; }
        }
        
        private int readCursor = 0;
        public TCPClientInfo ReadFirst()
        {
            lock (m_lock)
            {
                readCursor = 0;
                if (Clients.Count > 0)
                    return Clients[readCursor++];
                return null;
            }
        }
        /// <summary>
        /// 调用此方法前应该先调用 ReadFirst
        /// </summary>
        /// <returns></returns>
        public TCPClientInfo ReadNext()
        {
            lock (m_lock)
            {
                if (readCursor < Clients.Count)
                    return Clients[readCursor++];
                return null;
            }
        }
        
        public void AddClient(TCPClientInfo clientinfo)
        {
            lock (m_lock)
            {
                if (clientinfo != null)
                    Clients.Add(clientinfo);
            }
        }

        public void CloseClient(TCPClientInfo clientinfo)
        {
            lock (m_lock)
            {
                if (clientinfo != null && clientinfo.WorkSocket != null && clientinfo.WorkSocket.Connected)
                {
                    clientinfo.WorkSocket.Shutdown(SocketShutdown.Both);
                    clientinfo.WorkSocket.Close();
                    clientinfo.State = ClientState.Disconnected;
                    if (Clients.Contains(clientinfo)) Clients.Remove(clientinfo);
                }
            }
        }

        public void RemoveClient(Socket socket)
        {
            lock (m_lock)
            {
                for (int i = 0; i < Clients.Count; i++)
                {
                    if (Clients[i].WorkSocket == socket)
                    {
                        Clients.RemoveAt(i);
                        if (i < readCursor) readCursor--;
                        break;
                    }
                }
            }
        }

        public void CloseAll()
        {
            lock (m_lock)
            {
                foreach (var clientinfo in Clients)
                {
                    if (clientinfo != null && clientinfo.WorkSocket != null && clientinfo.WorkSocket.Connected)
                    {
                        clientinfo.WorkSocket.Shutdown(SocketShutdown.Both);
                        clientinfo.WorkSocket.Close();
                        clientinfo.State = ClientState.Disconnected;
                    }
                }
                Clients.Clear();
            }
        }

    }
}
