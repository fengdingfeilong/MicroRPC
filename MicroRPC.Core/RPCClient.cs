using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public class RPCClient
    {
        private Socket workSocket;
        private PackageHelper packageHelper;
        private int xid = 1;
        private Object m_lock = new Object();

        private List<Package> _replyPackages = new List<Package>();
        private List<int> timeoutPackages = new List<int>();

        public RPCClient()
        {
        }

        public bool Connect(string ipstring, int port)
        {
            workSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            packageHelper = new PackageHelper();
            packageHelper.PackageArrived += packageHelper_PackageArrived;
            try
            {
                workSocket.Connect(new IPEndPoint(IPAddress.Parse(ipstring), port));
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        /// <summary>
        /// Exec the rpc method
        /// </summary>
        /// <param name="rpcobj">rpcobj</param>
        /// <param name="timeout">Milliseconds</param>
        /// <returns></returns>
        public RPCObject RequestMethod(RPCObject rpcobj, int timeout)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var buffer = rpcobj.SerializeToJsonData();
            var package = new Package();
            lock (m_lock)//保证某个客户端多次并发请求时xid不一样
            {
                package.xid = xid;
                xid++;
                if (xid == int.MaxValue) xid = 1;
            }
            package.type = (byte)PackageType.Method;
            package.code = (byte)PackageCode.Normal;
            package.length = buffer.Length;
            package.data = buffer;
            var data = packageHelper.PackData(package);
            try
            {
                workSocket.Send(data);
            }
            catch (Exception ex)
            {
                return new RPCObject(null, null, null) { ExecError = true, ErrorMsg = "SendError :  " + ex.Message };
            }
            byte[] tempbuff = new byte[1460];
            while (true)
            {
                if (watch.ElapsedMilliseconds > timeout)
                {
                    timeoutPackages.Add(package.xid);
                    return new RPCObject(null, null, null) { ExecError = true, ErrorMsg = "Time Out" };
                }
                int count = workSocket.Receive(tempbuff);
                packageHelper.Parse(tempbuff, count);
                if (_replyPackages.Exists(p => p.xid == package.xid))
                {
                    var temppack = _replyPackages.First(p => p.xid == package.xid);
                    _replyPackages.Remove(temppack);
                    if (temppack.code == (int)PackageCode.Normal)
                        return RPCObject.DeserializeFromJsonData(temppack.data);
                    else
                        return new RPCObject(null, null, null) { ExecError = true, ErrorMsg = Encoding.UTF8.GetString(temppack.data) };
                }
            }
        }

        void packageHelper_PackageArrived(object sender, object e)
        {
            var package = (Package)e;
            if (!timeoutPackages.Contains(package.xid))
                _replyPackages.Add(package);
            else
                timeoutPackages.Remove(package.xid);
        }

        public void Close()
        {
            if (workSocket != null)
            {
                try
                {
                    workSocket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                workSocket.Close();
                workSocket = null;
            }
        }

    }
}
