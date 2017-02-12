using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroRPC.Core
{
    public enum PackageType { BeatHeart, Method, Command }
    public enum PackageCode { Normal, Nonexistent, Error }
    public struct Package
    {
        public int xid;//请求报文id
        public byte type;//消息类型，0表示心跳，1表示方法请求（涉及到引用类型），2表示命令请求（轻量级的请求）
        public byte code;//响应状态码，0表示正常，1表示服务或命令不存在，2表示出错(出错信息将存储在data中)
        public int length;//消息体长度
        public byte[] data;//消息体
    }

    public interface IDataParse
    {
        void Parse(byte[] buffer, int length);
        event EventHandler<Object> PackageArrived;
    }

    public class PackageHelper : IDataParse
    {
        private Object _tag;
        //用来存储其他信息
        public Object Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        public PackageHelper()
        { }

        private enum ParseState { XID, TYPE, CODE, LENGTH, DATA }
        private ParseState _state = ParseState.XID;

        private byte[] _tempBuffer = new byte[4];
        private int _tempindex = 0;
        private Package _tempPackage = new Package();
        public event EventHandler<Object> PackageArrived;
        public void Parse(byte[] buffer, int length)
        {
            for (int i = 0; i < length; i++)
            {
                switch (_state)
                {
                    case ParseState.XID:
                        _tempBuffer[_tempindex++] = buffer[i];
                        if (_tempindex == 4)
                        {
                            if (!BitConverter.IsLittleEndian)
                                _tempBuffer = (byte[])_tempBuffer.Reverse();
                            _tempPackage.xid = BitConverter.ToInt32(_tempBuffer, 0);
                            _state = ParseState.TYPE;
                            _tempindex = 0;
                        }
                        break;
                    case ParseState.TYPE:
                        _tempPackage.type = buffer[i];
                        _state = ParseState.CODE;
                        break;
                    case ParseState.CODE:
                        _tempPackage.code = buffer[i];
                        _state = ParseState.LENGTH;
                        break;
                    case ParseState.LENGTH:
                        _tempBuffer[_tempindex++] = buffer[i];
                        if (_tempindex == 4)
                        {
                            if (!BitConverter.IsLittleEndian) _tempBuffer = (byte[])_tempBuffer.Reverse();
                            _tempPackage.length = BitConverter.ToInt32(_tempBuffer, 0);
                            if (_tempPackage.length > 0)
                            {
                                _tempPackage.data = new byte[_tempPackage.length];
                                _state = ParseState.DATA;
                            }
                            else// no data or error
                            {
                                if (PackageArrived != null) PackageArrived(this, _tempPackage);
                                Reset();
                            }
                            _tempindex = 0;
                        }
                        break;
                    case ParseState.DATA:
                        _tempPackage.data[_tempindex++] = buffer[i];
                        if (_tempindex == _tempPackage.length)
                        {
                            if (PackageArrived != null) PackageArrived(this, _tempPackage);
                            Reset();
                        }
                        break;
                }
            }
        }

        public byte[] PackData(Package package)
        {
            if (package.length < 0) return null;
            byte[] buffer = new byte[10 + package.length];
            var tempbuffer = BitConverter.GetBytes(package.xid);
            if (!BitConverter.IsLittleEndian) tempbuffer = (byte[])tempbuffer.Reverse();//先发低字节
            tempbuffer.CopyTo(buffer, 0);
            buffer[4] = package.type;
            buffer[5] = package.code;
            tempbuffer = BitConverter.GetBytes(package.length);
            if (!BitConverter.IsLittleEndian) tempbuffer = (byte[])tempbuffer.Reverse();
            tempbuffer.CopyTo(buffer, 6);
            if (package.length > 0)
                package.data.CopyTo(buffer, 10);
            return buffer;
        }
        
        protected void Reset()
        {
            _tempindex = 0;
            _state = ParseState.XID;
            _tempPackage = new Package();
        }

    }

}
