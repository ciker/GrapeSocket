using System;
using System.Net.Sockets;
using GrapeSocket.Core.Interface;
using GrapeSocket.Core.Protocol;

namespace GrapeSocket.Server.Interface
{
    public interface ITcpPacketProtocol
    {
        /// <summary>
        /// 归属session
        /// </summary>
        ITcpSession Session { get; set; }
        /// <summary>
        /// 发送指令
        /// </summary>
        /// <returns></returns>
        void SendAsync(SendData data);
        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        /// <param name="receiveBuffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        bool ProcessReceiveBuffer(byte[] receiveBuffer, int offset, int count);
        /// <summary>
        /// 继续处理需要发送的数据
        /// </summary>
        void SendProcess();
        /// <summary>
        /// 清理协议管理器
        /// </summary>
        void Clear();
    }
}
