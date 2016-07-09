using System;
using GrapeSocket.Core.Session;
using System.Net;
using System.Net.Sockets;
using GrapeSocket.Client.Protocol;
using GrapeSocket.Core;
using GrapeSocket.Core.Protocol;
using GrapeSocket.Core.Interface;
using GrapeSocket.Client.Interface;

namespace GrapeSocket.Client
{
    public class TcpClientSession : ITcpClientSession
    {
        EndPoint remoteEndPoint;
        byte[] receiveBuffer;
        ILoger loger;
        object closeLock = new object();
        public TcpClientSession(EndPoint remoteEndPoint, int bufferSize, ILoger loger)
        {
            this.loger = loger;
            this.remoteEndPoint = remoteEndPoint;
            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.RemoteEndPoint = remoteEndPoint;
            SendEventArgs = new SocketAsyncEventArgs();
            SendEventArgs.RemoteEndPoint = remoteEndPoint;
            SendEventArgs.Completed += SendComplate;//数据发送完成事件
            receiveBuffer = new byte[bufferSize];
        }
        public DateTime? ActiveDateTime
        {
            get;set;
        }

        public DateTime? ConnectDateTime
        {
            get;set;
        }
        Socket connectSocket;
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
        private ITcpClientPacketProtocol packetProtocol;
        public ITcpClientPacketProtocol PacketProtocol
        {
            get {
                return packetProtocol;
            }
            set {
                packetProtocol = value;
                PacketProtocol.Session = this;
            }
        }

        public SocketAsyncEventArgs ReceiveEventArgs
        {
            get;set;
        }

        public SocketAsyncEventArgs SendEventArgs
        {
            get;set;
        }

        public uint SessionId
        {
            get;set;
        }

        public ITcpClientSessionPool Pool
        {
            get; set;
        }

        private Socket localSocket;
        public void Connect()
        {
            if (localSocket == null)
                localSocket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ReceiveEventArgs.Completed += ConnectComplate;
            localSocket.ConnectAsync(ReceiveEventArgs);
        }
        private void ConnectComplate(object sender, SocketAsyncEventArgs asyncEventArgs)
        {
            ReceiveEventArgs.Completed -= ConnectComplate;
            if (asyncEventArgs.SocketError == SocketError.Success)
            {
                ReceiveEventArgs.Completed += ReceiveComplate;
                ReceiveEventArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                ConnectSocket = asyncEventArgs.ConnectSocket;
                StartReceiveAsync();
                OnConnected(this);//响应连接成功事件
            }
            else
            {
                loger.Error(string.Format("连接{0}失败",remoteEndPoint));
            }
        }
        private void ReceiveComplate(object sender, SocketAsyncEventArgs receiveEventArgs)
        {
            if (receiveEventArgs.BytesTransferred > 0 && receiveEventArgs.SocketError == SocketError.Success)
            {
                try
                {
                    ActiveDateTime = DateTime.Now;
                    if (!PacketProtocol.ProcessReceiveBuffer(receiveEventArgs.Buffer, receiveEventArgs.Offset, receiveEventArgs.BytesTransferred))
                    { //如果处理数据返回失败，则断开连接
                        DisConnect();
                    }
                    StartReceiveAsync();//再次等待接收数据
                }
                catch (Exception e)
                {
                    DisConnect();
                    loger.Error(e);
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
                    PacketProtocol.SendProcess();//继续发送
            }
            else
            {
                lock (closeLock)
                {
                    if (ConnectSocket != null)
                        DisConnect();
                }
            }
        }
        public void SendAsync(byte[] data)
        {
            SendData sendData = new SendData() { Data = data };
            PacketProtocol.SendAsync(sendData);
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
                loger.Fatal(e);
            }
        }
        public void DisConnect()
        {
            OnDisConnect(this);
            if (ConnectSocket != null)
            {
                try
                {
                    ConnectSocket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception e)
                {
                    //日志记录
                    loger.Fatal(string.Format("CloseClientSocket Disconnect client {0} error, message: {1}", ConnectSocket, e.Message));
                }
                ConnectSocket.Dispose();
                ConnectSocket = null;
            }
            Clear();
            ReceiveEventArgs.Dispose();
            SendEventArgs.Dispose();
        }
        public void Clear()
        {
            //释放引用，并清理缓存，包括释放协议对象等资源
            PacketProtocol.Clear();
        }
        public void Dispose()
        {
            if (ConnectDateTime != null)
            {
                lock (closeLock)
                {
                    if (ConnectDateTime != null)
                    {
                        Clear();
                        ConnectDateTime = null;
                        ActiveDateTime = null;
                        if (Pool != null)
                        {
                            Pool.Push(this);
                        }
                        else
                        {
                            ReceiveEventArgs.Dispose();
                            SendEventArgs.Dispose();
                        }
                    }
                }
            }
        }
        //当接收到命令包时触发
        public virtual void OnReceived(ITcpClientSession session, IDynamicBuffer dataBuffer)
        {

        }
        public virtual void OnConnected(ITcpClientSession session)
        {

        }
        //断开连接事件
        public virtual void OnDisConnect(ITcpClientSession session)
        {

        }
    }
}
