# MicroRPC

MicroRPC is a very light rpc library and easy to extension. The Core just has about 1000 lines code.

MicroRPC  use TCP and json to transport. It's very easy to use.Here is the example.

1. define contract

   namespace MicroRPC.ContractDemo
   {

   ```c#
   public interface ICalculate
   {
       int Add(int a, int b);
       int Sub(int a, int b);
       int Mul(int a, int b);
       int Div(int a, int b);
   }
   ```
   }

   ​

2.  service implement the contract

   namespace MicroRPC.ServerDemo
   {

   ```c#
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
   ```
   }

   ​

3. start server

   namespace MicroRPC.ServerDemo
   {

   ```c#
   class Program
   {
       static void Main(string[] args)
       {
           try
           {
               var server = new RPCServer();
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
   ```
   }

   ​

4. client proxy

   namespace MicroRPC.ClientDemo
   {

   ```C#
   class WrapHelper
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
   ```
   ```c#
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
   ```
   }

   ​

5. client invoke

   namespace MicroRPC.ClientDemo
   {

   ```c#
   class Program
   {
       static void Main(string[] args)
       {
           MicroRPC.Core.RPCClient client = new Core.RPCClient();
           client.Connect("127.0.0.1", 9006);
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
   ```
   }
