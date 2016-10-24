using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public enum ClientState
    {
        Normal,
        Disconnected
    }
    /// <summary>
    /// infomation of connected client
    /// </summary>
    public class TCPClientInfo
    {
        public TCPClientInfo(Socket socket)
        {
            if (socket == null) throw new ArgumentNullException();
            WorkSocket = socket;
        }

        private Socket _workSocket;
        public Socket WorkSocket
        {
            get { return _workSocket; }
            set { _workSocket = value; }
        }

        private int _connectfFrequency;
        /// <summary>
        /// connect  count in 1 minute, if greate than 60, you may need to cut down this connection
        /// </summary>
        public int ConnectFrequency
        {
            get { return _connectfFrequency; }
            set { _connectfFrequency = value; }
        }

        private int _senddataFreuency;
        /// <summary>
        /// data bytes count in 1 minute, if greate than 60*1024*1024, you may need to cut down this connection
        /// </summary>
        public int SendDataFreuency
        {
            get { return _senddataFreuency; }
            set { _senddataFreuency = value; }
        }

        private int _noDataTime;
        /// <summary>
        /// minutes after last received data from this client, you can use this value to judge if the client has disconnected
        /// </summary>
        public int NoDataTime
        {
            get { return _noDataTime; }
            set { _noDataTime = value; }
        }

        private ClientState _state;
        public ClientState State
        {
            get { return _state; }
            set { _state = value; }
        }

    }
}
