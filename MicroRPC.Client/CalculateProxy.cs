using MicroRPC.ContractDemo;
using MicroRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ClientDemo
{
    class CalculateProxy : ICalculate
    {
        private RPCClient m_client;
        private int m_timeout;
        public CalculateProxy(RPCClient client, int timeout = 5000)
        {
            m_client = client;
            m_timeout = timeout;
        }

        public int Add(int a, int b)
        {
            var obj = WrapHelper.CheckExeError(() =>
            {
                return m_client.RequestMethod(new Core.RPCObject("ICalculate", "Add", new Parameter[] { new Parameter(typeof(int), a), new Parameter(typeof(int), b) }), m_timeout);
            });
            return (int)Convert.ChangeType(obj.ReturnValue, typeof(int));
        }

        public int Sub(int a, int b)
        {
            var obj = WrapHelper.CheckExeError(() =>
            {
                return m_client.RequestMethod(new Core.RPCObject("ICalculate", "Sub", new Parameter[] { new Parameter(typeof(int), a), new Parameter(typeof(int), b) }), m_timeout);
            });
            return (int)Convert.ChangeType(obj.ReturnValue, typeof(int));
        }

        public int Mul(int a, int b)
        {
            var obj = WrapHelper.CheckExeError(() =>
            {
                return m_client.RequestMethod(new Core.RPCObject("ICalculate", "Mul", new Parameter[] { new Parameter(typeof(int), a), new Parameter(typeof(int), b) }), m_timeout);
            });
            return (int)Convert.ChangeType(obj.ReturnValue, typeof(int));
        }

        public int Div(int a, int b)
        {
            var obj = WrapHelper.CheckExeError(() =>
            {
                return m_client.RequestMethod(new Core.RPCObject("ICalculate", "Div", new Parameter[] { new Parameter(typeof(int), a), new Parameter(typeof(int), b) }), m_timeout);
            });
            return (int)Convert.ChangeType(obj.ReturnValue, typeof(int));
        }
    }
}
