using MicroRPC.ContractDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ServerDemo
{
    public class HelloService : IHello
    {
        public void SayHello()
        {
            Console.WriteLine("Hello");
            //throw new Exception("this is an exception on server");
        }

        public string RelectText(string text)
        {
            return text;
        }
    }
}
