using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ClientDemo
{
    public class WrapHelper
    {
        public static MicroRPC.Core.RPCObject CheckExeError(Func<MicroRPC.Core.RPCObject> func)
        {
            var result = func();
            if (result != null && result.ExecError)
            {
                throw new Exception("Execute Error\r\n" + result.ErrorMsg);
            }
            return result;
        }
    }
}
