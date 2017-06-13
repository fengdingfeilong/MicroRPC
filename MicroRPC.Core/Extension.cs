using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public static class Extension
    {
        /// <summary>
        /// Test whether the socket is connected or not, find detail in msdn Socket.Connected
        /// </summary>
        public static bool IsConnected(this Socket socket)
        {
            bool blockingState = socket.Blocking;
            try
            {
                byte[] tmp = new byte[1];
                socket.Blocking = false;
                socket.Send(tmp, 0, 0);
            }
            catch (SocketException e)
            {
                if (e.NativeErrorCode.Equals(10035))// 10035 == WSAEWOULDBLOCK
                    Console.WriteLine("Still Connected, but the Send would block");
                else
                    Console.WriteLine("Disconnected: error code {0}!", e.NativeErrorCode);
            }
            finally
            {
                try
                {
                    socket.Blocking = blockingState;
                }
                catch
                { }
            }
            return socket.Connected;
        }
    }
}
