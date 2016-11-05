using System;
using System.Net.Sockets;
using GrapeSocket.Core.Interface;


namespace GrapeSocket.Client.Interface
{
    public interface ITcpClientSession :IDisposable
    {
        uint SessionId { get; set; }
        /// <summary>
        /// 所在池
        /// </summary>
        ITcpClientSessionPool Pool { get; set; }
        /// <summary>
        /// 连接时间
        /// </summary>
        DateTime? ConnectDateTime { get; set; }
        /// <summary>
        /// 最后活动时间
        /// </summary>
        DateTime? ActiveDateTime { get; set; }
        /// <summary>
        /// 连接套接字
        /// </summary>
        Socket ConnectSocket { get; set; }
        /// <summary>
        /// 包协议解析器
        /// </summary>
        ITcpClientPacketProtocol PacketProtocol { get; set; }
        /// <summary>
        /// 接收数据
        /// </summary>
        SocketAsyncEventArgs ReceiveEventArgs { get; set; }
        /// <summary>
        /// 发送数据
        /// </summary>
        SocketAsyncEventArgs SendEventArgs { get; set; }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        void SendAsync(byte[] data);
        /// <summary>
        /// 开始接收数据
        /// </summary>
        void StartReceiveAsync();
        /// <summary>
        /// 发送完成通知
        /// </summary>
        void SendComplate();
        /// <summary>
        /// 连接
        /// </summary>
        void Connect();
        /// <summary>
        /// 断开连接
        /// </summary>
        void DisConnect();
        /// <summary>
        /// 清理但不断开连接
        /// </summary>
        void Clear();
        void OnReceived(ITcpClientSession session, IDynamicBuffer dataBuffer);
        /// <summary>
        /// 当连接请求通过后
        /// </summary>
        void OnConnected(ITcpClientSession session);
        /// <summary>
        /// 断开连接通知
        /// </summary>
        void OnDisConnect(ITcpClientSession session);
        ILoger GetLoger();
    }
}
