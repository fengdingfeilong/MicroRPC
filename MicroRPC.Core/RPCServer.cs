using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public class RPCServer
    {
        private int m_port = 9006;
        private int m_maxconnection = 10000;
        private TCPServer tcpServer;

        public RPCServer()
        {
            InitServer();
        }
        public RPCServer(int port, int maxconnection)
        {
            m_port = port;
            m_maxconnection = maxconnection;
            InitServer();
        }

        private Hashtable serviceMap = new Hashtable();
        public void PubService(string interfaceName, Type service)
        {
            serviceMap.Add(interfaceName, service);
        }

        private void InitServer()
        {
            tcpServer = new TCPServer(m_port, m_maxconnection);
            tcpServer.Open();
            tcpServer.NewClientAccepted += TcpServer_NewClientAccepted;
            tcpServer.ClientDisconnected += TcpServer_ClientDisconnected;
            tcpServer.DataReceived += TcpServer_DataReceived;
            tcpServer.DataSended += TcpServer_DataSended;
            tcpServer.ServerClosed += TcpServer_ServerClosed;
        }

        private void TcpServer_NewClientAccepted(object sender, System.Net.Sockets.Socket e)
        {

        }
        private void TcpServer_DataReceived(object sender, SocketDataEventArgs e)
        {
            if (e.WorkSocket == null || !e.WorkSocket.Connected) return;
            if (e.DataParse == null) e.DataParse = new PackageHelper() { WorkSocket = e.WorkSocket };
            e.DataParse.PackageArrived += DataParse_PackageArrived;
            e.DataParse.Parse(e.Data, e.Data.Length);
        }

        void DataParse_PackageArrived(object sender, object e)
        {
            var packageHelper = (PackageHelper)sender;
            var package = (Package)e;
            switch (package.type)
            {
                case (int)PackageType.BeatHeart://beatheart
                    HandleBeatHeart(packageHelper, package);
                    break;
                case (int)PackageType.Command://command mode
                    HandleCommand(packageHelper, package);
                    break;
                case (int)PackageType.Method://method mode
                    HandleMethod(packageHelper, package);
                    break;
            }
        }

        private void HandleBeatHeart(PackageHelper packageHelper, Package package)
        {

        }
        private void HandleCommand(PackageHelper packageHelper, Package package)
        {

        }
        ///use json data for transport
        private void HandleMethod(PackageHelper packageHelper, Package package)
        {
            var rpcobj = RPCObject.DeserializeFromJsonData(package.data);
            string errortext = string.Empty;
            if (rpcobj != null)
            {
                try
                {
                    if (serviceMap.Contains(rpcobj.ExecInterface))
                    {
                        rpcobj.RealExec((Type)serviceMap[rpcobj.ExecInterface]);
                        var buffer = rpcobj.SerializeToJsonData();
                        package.code = (byte)PackageCode.Normal;
                        package.length = buffer.Length;
                        package.data = buffer;
                    }
                    else
                    {
                        errortext = "can not find the service";
                        package.code = (byte)PackageCode.Nonexistent;
                    }
                }
                catch (Exception ex)
                {
                    errortext = ex.Message;
                    if (ex.InnerException != null)
                        errortext = ex.InnerException.Message;
                    package.code = (byte)PackageCode.Error;
                    Console.WriteLine(errortext);
                }
            }
            else
                errortext = "can not deserialize the rpc object";
            if (!string.IsNullOrEmpty(errortext))
            {
                package.data = Encoding.UTF8.GetBytes(errortext);
                package.length = package.data.Length;
            }
            SendPackage(packageHelper, package);
        }

        private void SendPackage(PackageHelper packageHelper, Package package)
        {
            var dataargs = new SocketDataEventArgs();
            dataargs.WorkSocket = packageHelper.WorkSocket;
            dataargs.Data = packageHelper.PackData(package);
            tcpServer.SendData(dataargs);
        }

        private void TcpServer_DataSended(object sender, int e)
        {

        }
        private void TcpServer_ClientDisconnected(object sender, System.Net.Sockets.Socket e)
        {

        }
        private void TcpServer_ServerClosed(object sender, EventArgs e)
        {

        }


    }
}
