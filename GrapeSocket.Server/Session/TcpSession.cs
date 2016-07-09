using System;
using GrapeSocket.Core.Session;
using System.Net.Sockets;
using GrapeSocket.Core;
using GrapeSocket.Server.Interface;
using GrapeSocket.Core.Interface;

namespace GrapeSocket.Server.Session
{
    public class TcpSession : ITcpSession
    {
        object closeLock = new object();
        public TcpSession()
        {
            SessionData = new DataContainer();
            ReceiveEventArgs = new SocketAsyncEventArgs();
            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.Completed += SendComplate;//数据发送完成事件
            ReceiveEventArgs.Completed += ReceiveComplate;
        }
        byte[] receiveBuffer;
        public byte[] ReceiveBuffer
        {
            get {
                return receiveBuffer;
            }
            set
            {
                receiveBuffer = value;
                ReceiveEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            }
        }
        public uint SessionId {get; set;}
        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime? ConnectDateTime { get; set; }
        /// <summary>
        /// 上次活动时间
        /// </summary>
        public DateTime ActiveDateTime { get; set; }
        Socket connectSocket;
        /// <summary>
        /// 连接套接字
        /// </summary>
        public Socket ConnectSocket
        {
            get { return connectSocket; }
            set
            {
                connectSocket = value;
                if (connectSocket == null) //清理缓存
                {
                    PacketProtocol.Clear();
                }
                ReceiveEventArgs.AcceptSocket = connectSocket;
                SendEventArgs.AcceptSocket = connectSocket;
            }
        }
        /// <summary>
        /// 接受数据
        /// </summary>
        public SocketAsyncEventArgs ReceiveEventArgs{get;set;}
        /// <summary>
        /// 发送数据
        /// </summary>
        public SocketAsyncEventArgs SendEventArgs{get;set;}
        ITcpPacketProtocol packetProtocol;
        //包接收发送处理器
        public ITcpPacketProtocol PacketProtocol
        {
            get {
                return packetProtocol;
            }
            set {
                packetProtocol = value;
                packetProtocol.Session = this;
            }
        }

        public DataContainer SessionData
        {
            get; set;
        }

        public ITcpSessionPool Pool
        {
            get;
            set;
        }

        /// <summary>
        /// 发送指令
        /// </summary>
        /// <param name="data"></param>
        public void SendAsync(byte[] data)
        {
            PacketProtocol.SendAsync(data);
        }

        public void StartReceiveAsync()
        {
            try
            {
                bool willRaiseEvent = ConnectSocket.ReceiveAsync(ReceiveEventArgs); //投递接收请求
                if (!willRaiseEvent)
                {
                    ReceiveComplate(null, ReceiveEventArgs);
                }
            }
            catch (Exception e)
            {
               Pool.TcpServer.Loger.Fatal(e);
            }
        }
        private void ReceiveComplate(object sender, SocketAsyncEventArgs receiveEventArgs)
        {
            if (receiveEventArgs.BytesTransferred > 0 && receiveEventArgs.SocketError == SocketError.Success)
            {
                ActiveDateTime = DateTime.Now;
                try
                {
                    if (!PacketProtocol.ProcessReceiveBuffer(receiveEventArgs.Buffer, receiveEventArgs.Offset, receiveEventArgs.BytesTransferred))
                    { //如果处理数据返回失败，则断开连接
                        DisConnect();
                    }
                    StartReceiveAsync();//再次等待接收数据
                }
                catch (Exception e)
                {
                    DisConnect();
                    Pool.TcpServer.Loger.Error(e);
                }
            }
            else
            {
                DisConnect();
            }
        }
        public void SendComplate()
        {
            SendComplate(null, SendEventArgs);
        }
        private void SendComplate(object sender, SocketAsyncEventArgs sendEventArgs)
        {
            if (sendEventArgs.SocketError == SocketError.Success)
            {
                if (ConnectSocket != null)
                {
                    PacketProtocol.SendProcess();//继续发送
                }
            }
            else
            {
                lock (closeLock)
                {
                    DisConnect();
                }
            }
        }
        //断开连接
        public virtual void DisConnect()
        {
            if (ConnectDateTime != null)
            {
                lock (closeLock)
                {
                    if (ConnectDateTime != null)
                    {
                        if (Pool != null)
                        {
                            _DisConnect();
                            Clear();
                            Pool.Push(this);
                        }
                        else
                        {
                            Dispose();
                        }
                    }
                }
            }
        }
        private void _DisConnect()
        {
            Pool.TcpServer.OnDisConnect(this);
            ConnectDateTime = null;
            if (ConnectSocket != null)
            {
                try
                {
                    ConnectSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    //日志记录
                    Pool.TcpServer.Loger.Fatal(string.Format("CloseClientSocket Disconnect client {0} error, message: {1}", ConnectSocket, e.Message));
                }
                ConnectSocket.Dispose();
                ConnectSocket = null;
            }
        }
        //清理session
        public virtual void Clear()
        {
            //释放引用，并清理缓存，包括释放协议对象等资源
            PacketProtocol.Clear();
            SessionData.Clear();//清理session数据
        }

        public virtual void Dispose()
        {
            _DisConnect();
            Clear();
            ReceiveEventArgs.Dispose();
            SendEventArgs.Dispose();
        }
    }
}
