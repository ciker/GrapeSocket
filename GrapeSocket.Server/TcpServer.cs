using System;
using System.Net;
using System.Net.Sockets;
using GrapeSocket.Core.Interface;
using System.Collections.Concurrent;
using GrapeSocket.Server.Interface;
using GrapeSocket.Server.Session;
using GrapeSocket.Server.Protocol;
using GrapeSocket.Server.Config;

namespace GrapeSocket.Server
{
    public abstract class TcpServer : ITcpServer
    {
        private ITcpSessionPool sessionPool;
        private IPEndPoint endPoint;
        public Socket ListenerSocket { get; set; }
        //构造函数
        public TcpServer(TcpServerConfig config)
        {
            this.Config = config;
            endPoint = new IPEndPoint(IPAddress.Parse(config.IP), config.Port);
            this.sessionPool = new TcpSessionPool();
            this.sessionPool.TcpServer = this;
            this.Loger = GetLoger();
        }
        public TcpServerConfig Config { get; set; }
        public uint ServerId { get { return Config.ServerId; } }
        public string ServerName { get { return Config.Name; } }
        public ITcpSessionPool SessionPool { get { return sessionPool; } }
        public ConcurrentDictionary<uint, ITcpSession> OnlineList
        {
            get
            {
                return sessionPool.ActiveList;
            }
        }
        public ILoger Loger { get; set; }
        public virtual void Start()
        {
            ListenerSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            ListenerSocket.Bind(endPoint);
            ListenerSocket.Listen(Config.BackLog);
            StartAccept(null);
        }

        public virtual void Stop()
        {
            if (ListenerSocket != null)
            {
                ListenerSocket.Dispose();
                ListenerSocket = null;
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            ITcpSession session = sessionPool.Pop();
            if (session != null)
            {
                session.ConnectSocket = acceptEventArgs.AcceptSocket;
                OnConnected(session);//启动连接请求通过事件
                session.StartReceiveAsync();//开始接收数据
            }
            else
            {
                Loger.Warning("TcpServer:session is null");
                acceptEventArgs.AcceptSocket.Dispose();
            }
            StartAccept(acceptEventArgs); //把当前异步事件释放，等待下次连接
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            if (ListenerSocket != null)
            {
                if (acceptEventArgs == null)
                {
                    acceptEventArgs = new SocketAsyncEventArgs();
                    acceptEventArgs.Completed += AcceptCompleted;
                }
                else
                {
                    acceptEventArgs.AcceptSocket = null; //释放上次绑定的Socket，等待下一个Socket连接
                }
                bool willRaiseEvent = ListenerSocket.AcceptAsync(acceptEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessAccept(acceptEventArgs);
                }
            }
        }
        private void AcceptCompleted(object sender, SocketAsyncEventArgs acceptEventArgs)
        {
            try
            {
                ProcessAccept(acceptEventArgs);
            }
            catch (Exception e)
            {
                Loger.Fatal(e);
            }
        }
        //当接收到命令包时触发
        public virtual void OnReceived(ITcpSession session, IDynamicBuffer dataBuffer)
        {

        }
        public virtual void OnConnected(ITcpSession session)
        {

        }
        public virtual void OnError(ITcpSession seseion, Exception error)
        {

        }
        //断开连接事件
        public virtual void OnDisConnect(ITcpSession session) {
            
        }
        public virtual ITcpPacketProtocol GetProtocol()
        {
            return new TcpPacketProtocol(Config.NetByteOrder);
        }
        public abstract ILoger GetLoger();
    }
}
