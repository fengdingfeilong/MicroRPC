using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;

namespace MicroRPC.Core
{
    public class Parameter
    {
        public Type ParameterType;
        public Object ParameterValue;
        public Parameter(Type type, Object value)
        {
            ParameterType = type;
            ParameterValue = value;
        }
    }
    public class RPCObject
    {
        public string ExecInterface;
        public string ExecMethodName;
        public Parameter[] Args;
        public Object ReturnValue;
        [JsonIgnore]
        public bool ExecError = false;
        [JsonIgnore]
        public string ErrorMsg = string.Empty;
        public RPCObject(string execInterface, string execMethodName, Parameter[] args)
        {
            this.ExecInterface = execInterface;
            this.ExecMethodName = execMethodName;
            this.Args = args;
        }

        public byte[] SerializeToBinary()
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, this);
                return ms.GetBuffer();
            }
        }

        public static RPCObject DeserializeFromBinary(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                return (RPCObject)formatter.Deserialize(ms);
            }
        }

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static RPCObject DeserializeFromJson(string jsontext)
        {
            return JsonConvert.DeserializeObject<RPCObject>(jsontext);
        }

        public byte[] SerializeToJsonData()
        {
            string text = JsonConvert.SerializeObject(this);
            return Encoding.UTF8.GetBytes(text);
        }
        public static RPCObject DeserializeFromJsonData(byte[] jsondata)
        {
            string jsontext = Encoding.UTF8.GetString(jsondata);
            return JsonConvert.DeserializeObject<RPCObject>(jsontext);
        }

        public void RealExec(Type serviecType)
        {
            try
            {
                Object[] parameter = null;
                if (Args != null && Args.Count() > 0)
                {
                    parameter = new Object[Args.Count()];
                    for (int i = 0; i < Args.Count(); i++)
                        parameter[i] = Convert.ChangeType(Args[i].ParameterValue, Args[i].ParameterType);
                }
                ReturnValue = serviecType.GetMethod(ExecMethodName).Invoke(Activator.CreateInstance(serviecType), parameter);
            }
            catch
            {
                throw;
            }
        }

    }

}
