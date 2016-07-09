using System;
using System.Net.Sockets;
using GrapeSocket.Core.Interface;

namespace GrapeSocket.Client.Interface
{
    public interface ITcpClientPacketProtocol
    {
        /// <summary>
        /// 归属session
        /// </summary>
        ITcpClientSession Session { get; set; }
        /// <summary>
        /// 发送指令
        /// </summary>
        /// <returns></returns>
        void SendAsync(byte[] data);
        /// <summary>
        /// 处理接收数据
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
        /// 清空session
        /// </summary>
        void Clear();
    }
}
