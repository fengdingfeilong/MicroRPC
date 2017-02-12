using MicroRPC.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.PerformanceTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            List<Socket> sockets = new List<Socket>(10000);
            //for (int index = 0; index < 1000; index++)
            //{
            //    var workSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //    try
            //    {
            //        workSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9006));
            //        Console.WriteLine("client {0} connected", index);
            //        sockets.Add(workSocket);
            //        byte[] buffer = BitConverter.GetBytes(index);
            //        int c = workSocket.Send(buffer);
            //        //workSocket.Shutdown(SocketShutdown.Both);
            //        //workSocket.Close();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("client {0} connected failed ==========================", index);
            //        //throw ex;
            //    }
            //}
            Parallel.For(0, 10000, index =>
            {
                var workSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    workSocket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9006));
                    Console.WriteLine("client {0} connected", index);
                    sockets.Add(workSocket);
                    //byte[] buffer = Encoding.ASCII.GetBytes("FROM :   " + workSocket.LocalEndPoint.ToString());
                    byte[] buffer = new byte[1024];
                    workSocket.Send(buffer);
                    //System.Threading.Thread.Sleep(50);
                    buffer = new byte[1600];
                    workSocket.Send(buffer);
                    //workSocket.Shutdown(SocketShutdown.Both);
                    //workSocket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("client {0} connected failed ==========================", index);
                    //throw ex;
                }
            });
            Console.WriteLine(" {0}  ms", watch.ElapsedMilliseconds);
            Console.WriteLine("OVER");
            Console.ReadKey();
        }

    }
}
