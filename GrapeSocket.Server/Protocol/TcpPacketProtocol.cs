using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using GrapeSocket.Core;
using GrapeSocket.Core.Buffer;
using GrapeSocket.Core.Interface;
using GrapeSocket.Server.Interface;

namespace GrapeSocket.Server.Protocol
{
    public class TcpPacketProtocol : ITcpPacketProtocol
    {
        public TcpPacketProtocol(bool netByteOrder = false)
        {
            this.NetByteOrder = netByteOrder;
        }
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

        private byte[] noComplateBytes = null;//未完全发送指令
        private int noComplateOffset = 0;
        bool isSend = false;//发送状态
        private ConcurrentQueue<byte[]> sendDataQueue = new ConcurrentQueue<byte[]>();//指令发送队列
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
        public void SendAsync(byte[] data)
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
                if (noComplateBytes != null)
                {
                    int noComplateLength = noComplateBytes.Length - noComplateOffset;
                    if (noComplateLength <= surplus)
                    {
                        SendBuffer.WriteBuffer(noComplateBytes, noComplateOffset, noComplateLength);
                        surplus -= noComplateLength;
                        noComplateBytes = null;
                        noComplateOffset = 0;
                    }
                    else
                    {
                        SendBuffer.WriteBuffer(noComplateBytes, noComplateOffset, surplus);
                        noComplateOffset += surplus;
                        surplus -= surplus;
                        break;
                    }
                }
                if (surplus >= intByteLength)
                {
                    byte[] data;
                    if (sendDataQueue.TryDequeue(out data))
                    {
                        var PacketAllLength = data.Length + intByteLength;
                        if (PacketAllLength <= surplus)
                        {
                            SendBuffer.WriteInt(data.Length, NetByteOrder); //写入总大小
                            SendBuffer.WriteBuffer(data); //写入命令内容
                            surplus -= PacketAllLength;
                        }
                        else
                        {
                            SendBuffer.WriteInt(data.Length, NetByteOrder); //写入总大小
                            surplus -= intByteLength; ;
                            if (surplus > 0)
                            {
                                SendBuffer.WriteBuffer(data, 0, surplus); //写入命令内容
                                noComplateOffset = surplus;
                            }
                            noComplateBytes = data;//把未全部发送指令缓存
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
                    byte[] cmd;
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
            noComplateBytes = null;
            needReceivePacketLenght = 0;
        }
    }
}
