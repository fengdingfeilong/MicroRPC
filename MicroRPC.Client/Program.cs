using MicroRPC.ContractDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            MicroRPC.Core.RPCClient client = new Core.RPCClient();
            client.Connect("127.0.0.1", 9006);
            IHello hello = new HelloProxy(client);
            try
            {
                hello.SayHello();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                string s = hello.RelectText("hahaha");
                Console.WriteLine(s);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            ICalculate calculate = new CalculateProxy(client);
            try
            {
                int a = calculate.Add(1, 2);
                Console.WriteLine("1+2={0}", a);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                int a = calculate.Sub(1, 2);
                Console.WriteLine("1-2={0}", a);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                int a = calculate.Mul(3, 2);
                Console.WriteLine("3*2={0}", a);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                int a = calculate.Div(4, 2);
                Console.WriteLine("4/2={0}", a);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                int a = calculate.Div(4, 0);
                Console.WriteLine("4/0={0}", a);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            client.Close();
            Console.ReadKey();
        }
    }
}
