using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using GrapeSocket.Core.Buffer;
using GrapeSocket.Core.Protocol;
using GrapeSocket.Client.Interface;
using GrapeSocket.Core.Interface;

namespace GrapeSocket.Client.Protocol
{
    public class TcpClientPacketProtocol : ITcpClientPacketProtocol
    {
        object closeLock = new object();
        bool NetByteOrder = false;
        static int intByteLength = sizeof(int);
        private object clearLock = new object();
        //缓冲器池
        private static FixedBufferPool BufferPool;
        private int  needReceivePacketLenght;
        //数据发送缓冲器
        public IFixedBuffer SendBuffer { get; set; }
        private IDynamicBuffer ReceiveDataBuffer { get; set; }
        public ITcpClientSession Session
        {
            get;
            set;
        }

        private SendData NoComplateCmd = null;//未完全发送指令
        bool isSend = false;//发送状态
        private ConcurrentQueue<SendData> sendDataQueue = new ConcurrentQueue<SendData>();//指令发送队列
        public TcpClientPacketProtocol(int bufferSize, int fixedBufferPoolSize)
        {
            SendBuffer = new FixedBuffer(bufferSize);
            ReceiveDataBuffer = new DynamicBuffer(bufferSize);
        }
        public bool ProcessReceiveBuffer(byte[] receiveBuffer, int offset, int count)
        {
            while (count > 0)
            {
                if (needReceivePacketLenght == 0)
                {
                    //按照长度分包
                    needReceivePacketLenght = BitConverter.ToInt32(receiveBuffer, offset); //获取包长度
                    if (NetByteOrder)
                        needReceivePacketLenght = IPAddress.NetworkToHostOrder(needReceivePacketLenght); //把网络字节顺序转为本地字节顺序
                    offset += intByteLength;
                    count -= intByteLength;
                }
                if (count == 0) break;
                if (count > needReceivePacketLenght)
                {
                    ReceiveDataBuffer.WriteBuffer(receiveBuffer, offset, needReceivePacketLenght);
                    offset += needReceivePacketLenght;
                    count -= needReceivePacketLenght;
                    ReceiveData();
                }
                else if (count == needReceivePacketLenght)
                {
                    ReceiveDataBuffer.WriteBuffer(receiveBuffer, offset, needReceivePacketLenght);
                    ReceiveData();
                    break;
                }
                else
                {
                    ReceiveDataBuffer.WriteBuffer(receiveBuffer, offset, count);
                    needReceivePacketLenght -= count;
                    break;
                }
            }
            return true;
        }
        public void ReceiveData()
        {
            Session.OnReceived(Session, ReceiveDataBuffer);
            ReceiveDataBuffer.Clear();//清空数据接收器缓存
            needReceivePacketLenght = 0;
        }
        object lockObj = new object();
        public void SendAsync(SendData data)
        {
            sendDataQueue.Enqueue(data);
            if (!isSend)
            {
                lock (lockObj)
                {
                    if (!isSend)
                    {
                        isSend = true;
                        if (Session.ConnectSocket != null)
                        {
                            SendProcess();
                        }
                    }
                }
            }
        }
        void FlushBuffer(ref int surplus)
        {
            if(sendDataQueue.Count==0)
            while (sendDataQueue.Count > 0)
            {
                if (NoComplateCmd != null)
                {
                    int noComplateLength = NoComplateCmd.Data.Length - NoComplateCmd.Offset;
                    if (noComplateLength <= surplus)
                    {
                        SendBuffer.WriteBuffer(NoComplateCmd.Data, NoComplateCmd.Offset, noComplateLength);
                        surplus -= noComplateLength;
                        NoComplateCmd = null;
                    }
                    else
                    {
                        SendBuffer.WriteBuffer(NoComplateCmd.Data, NoComplateCmd.Offset, surplus);
                        NoComplateCmd.Offset += surplus;
                        surplus -= surplus;
                        break;
                    }
                }
                if (surplus >= intByteLength)
                {
                    SendData data;
                    if (sendDataQueue.TryDequeue(out data))
                    {
                        var PacketAllLength = data.Data.Length + intByteLength;
                        if (PacketAllLength <= surplus)
                        {
                            SendBuffer.WriteInt(data.Data.Length, false); //写入总大小
                            SendBuffer.WriteBuffer(data.Data); //写入命令内容
                            surplus -= PacketAllLength;
                        }
                        else
                        {
                            SendBuffer.WriteInt(data.Data.Length, false); //写入总大小
                            surplus -= intByteLength; ;
                            if (surplus > 0)
                            {
                                SendBuffer.WriteBuffer(data.Data, data.Offset, surplus); //写入命令内容
                                data.Offset = surplus;
                            }
                            NoComplateCmd = data;//把未全部发送指令缓存
                            break;
                        }
                    }
                }
            }
        }
        public void SendProcess()
        {
            SendBuffer.Clear(); //清除已发送的包
            int surplus = SendBuffer.Buffer.Length;
            FlushBuffer(ref surplus);
            if (surplus < SendBuffer.Buffer.Length)
            {
                if (Session.ConnectSocket != null)
                {
                    Session.SendEventArgs.SetBuffer(SendBuffer.Buffer, 0, SendBuffer.DataSize);
                    bool willRaiseEvent = Session.ConnectSocket.SendAsync(Session.SendEventArgs);
                    if (!willRaiseEvent)
                    {
                        Session.SendComplate();
                    }
                }
                else
                {
                    isSend = false;
                }
            }
            else
            {
                isSend = false;
            }
        }
        //断开连接
        private void DisConnect()
        {
            if (Session.ConnectSocket != null)
            {
                lock (closeLock)
                {
                    if (Session.ConnectSocket != null)
                        Session.DisConnect();
                }
            }
        }
        public void Clear()
        {
            SendBuffer.Clear();
            lock (clearLock)
            {
                isSend = false;
                if (sendDataQueue.Count > 0)
                {
                    SendData cmd;
                    while (sendDataQueue.TryDequeue(out cmd))
                    {
                    }
                }
            }
            NoComplateCmd = null;
            needReceivePacketLenght = 0;
        }
    }
}
