using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using GrapeSocket.Core;
using GrapeSocket.Core.Buffer;
using GrapeSocket.Core.Protocol;
using GrapeSocket.Core.Interface;
using GrapeSocket.Server.Interface;

namespace GrapeSocket.Server.Protocol
{
    public class TcpPacketProtocol : ITcpPacketProtocol
    {
        bool NetByteOrder = false;
        static int intByteLength = sizeof(int);
        private object clearLock = new object();
        private int needReceivePacketLenght;
        //数据发送缓冲器
        public IFixedBuffer SendBuffer { get; set; }
        private IDynamicBuffer ReceiveDataBuffer { get; set; }
        ITcpSession _session;
        public ITcpSession Session
        {
            get { return _session; }
            set {
                _session = value;
                SendBuffer = new FixedBuffer(value.Pool.TcpServer.Config.BufferSize);
                ReceiveDataBuffer = new DynamicBuffer(value.Pool.TcpServer.Config.BufferSize);
            }
        }

        private SendData NoComplateCmd = null;//未完全发送指令
        bool isSend = false;//发送状态
        private ConcurrentQueue<SendData> sendDataQueue = new ConcurrentQueue<SendData>();//指令发送队列
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
            Session.Pool.TcpServer.OnReceived(Session, ReceiveDataBuffer);
            ReceiveDataBuffer.Clear();//清空数据接收器缓存
            needReceivePacketLenght = 0;
        }
        object lockObj = new object();
        public void SendAsync(SendData data)
        {
            sendDataQueue.Enqueue(data);
            if (!isSend)
            {
                lock(lockObj)
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
            while(sendDataQueue.Count > 0)
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
        public void Clear()
        {
            lock (clearLock)
            {
                isSend = false;
                if (sendDataQueue.Count > 0)
                {
                    SendData cmd;
                    if (!sendDataQueue.TryDequeue(out cmd))
                    {
                        SpinWait spinWait = new SpinWait();
                        while (sendDataQueue.TryDequeue(out cmd))
                        {
                            spinWait.SpinOnce();
                        }
                    }
                }
            }
            SendBuffer.Clear();
            NoComplateCmd = null;
            needReceivePacketLenght = 0;
        }
    }
}
