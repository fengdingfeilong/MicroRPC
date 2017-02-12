using MicroRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicroRPC.PerformanceTestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TCPServer tcpServer = new TCPServer(9006, 10000);
            if (tcpServer.Open())
                Console.WriteLine("Server Opened");
            tcpServer.NewClientAccepted += TcpServer_NewClientAccepted;
            tcpServer.ClientDisconnected += TcpServer_ClientDisconnected;
            tcpServer.DataReceived += TcpServer_DataReceived;
            Console.ReadKey();
        }

        private static void TcpServer_DataReceived(object sender, SocketDataEventArgs e)
        {
            Console.WriteLine("received " + e.DataCount + " bytes");
        }

        private static void TcpServer_ClientDisconnected(object sender, Socket e)
        {
            Console.WriteLine(e.RemoteEndPoint.ToString() + "断开连接");
        }

        private static void TcpServer_NewClientAccepted(object sender, SocketDataEventArgs e)
        {
            Console.WriteLine(e.WorkSocket.RemoteEndPoint.ToString());
            //Thread.Sleep(500);
        }
    }
}
