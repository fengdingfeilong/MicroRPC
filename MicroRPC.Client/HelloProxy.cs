using MicroRPC.ContractDemo;
using MicroRPC.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ClientDemo
{
    class HelloProxy : IHello
    {
        private RPCClient m_client;
        private int m_timeout;
        public HelloProxy(RPCClient client, int timeout = 5000)
        {
            m_client = client;
            m_timeout = timeout;
        }

        public void SayHello()
        {
            WrapHelper.CheckExeError(() =>
            {
                return m_client.RequestMethod(new RPCObject("IHello", "SayHello", null), m_timeout);
            });
        }

        public string RelectText(string text)
        {
            var obj = WrapHelper.CheckExeError(() =>
            {
                return m_client.RequestMethod(new RPCObject("IHello", "RelectText", new Parameter[] { new Parameter(typeof(String), text) }), m_timeout);
            });
            return (string)Convert.ChangeType(obj.ReturnValue, typeof(string));
        }
    }
}
