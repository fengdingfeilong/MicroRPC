using MicroRPC.ContractDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ServerDemo
{
    class CalculateService : ICalculate
    {
        public int Add(int a, int b)
        {
            return a + b;
        }

        public int Sub(int a, int b)
        {
            return a - b;
        }

        public int Mul(int a, int b)
        {
            return a * b;
        }

        public int Div(int a, int b)
        {
            return a / b;
        }
    }
}
