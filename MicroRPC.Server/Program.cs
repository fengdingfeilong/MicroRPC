using MicroRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ServerDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var server = new RPCServer();
                server.PubService("IHello", typeof(HelloService));
                server.PubService("ICalculate", typeof(CalculateService));
                Console.WriteLine("Services are ready now.\r\nPress ESC to Exit the Server");                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Start Service Error:\r\n" + ex.Message);
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;
        }

    }
}
