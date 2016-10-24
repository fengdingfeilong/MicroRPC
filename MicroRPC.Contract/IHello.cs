using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.ContractDemo
{
    public interface IHello
    {
        void SayHello();
        string RelectText(string text);
    }
}
