using System;
using System.Net.Sockets;
using GrapeSocket.Core.Session;
using GrapeSocket.Core.Interface;

namespace GrapeSocket.Server.Interface
{
    public interface ITcpSession:IDisposable
    {
        uint SessionId { get; set; }
        /// <summary>
        /// 所在池
        /// </summary>
        ITcpSessionPool Pool { get; set; }
        /// <summary>
        /// 数据接收缓冲区
        /// </summary>
        byte[] ReceiveBuffer { get; set; }
        /// <summary>
        /// 连接时间
        /// </summary>
        DateTime? ConnectDateTime { get; set; }
        /// <summary>
        /// 最后活动时间
        /// </summary>
        DateTime ActiveDateTime { get; set; }
        /// <summary>
        /// session数据容器
        /// </summary>
        DataContainer SessionData { get; set; }
        /// <summary>
        /// 连接套接字
        /// </summary>
        Socket ConnectSocket { get; set; }
        /// <summary>
        /// 包协议解析器
        /// </summary>
        ITcpPacketProtocol PacketProtocol { get; set; }
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
        /// 清空session
        /// </summary>
        void Clear();
        /// <summary>
        /// 断开连接
        /// </summary>
        void DisConnect();
    }
}
