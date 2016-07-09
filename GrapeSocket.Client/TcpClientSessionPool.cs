using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using GrapeSocket.Core.Interface;
using GrapeSocket.Core;
using GrapeSocket.Client.Protocol;
using GrapeSocket.Client.Interface;

namespace GrapeSocket.Client
{
    public class TcpClientSessionPool : ITcpClientSessionPool
    {
        private ConcurrentQueue<ITcpClientSession> pool = new ConcurrentQueue<ITcpClientSession>();
        private ConcurrentDictionary<long, ITcpClientSession> activeDict = new ConcurrentDictionary<long, ITcpClientSession>();
        private int count = 0, bufferSize, maxSessions, fixedBufferPoolSize;
        ILoger loger;
        EndPoint remoteEndPoint;
        SessionId sessionId;
        public TcpClientSessionPool(uint serverId,EndPoint remoteEndPoint, int bufferSize,int fixedBufferPoolSize, int maxSessions, ILoger loger)
        {
            this.bufferSize = bufferSize;
            this.maxSessions = maxSessions;
            this.remoteEndPoint = remoteEndPoint;
            this.fixedBufferPoolSize = fixedBufferPoolSize;
            this.loger = loger;
            sessionId = new SessionId(serverId);
        }
        public int Count
        {
            get
            {
                return count;
            }
        }

        public int FreeCount
        {
            get
            {
                return pool.Count;
            }
        }

        public ConcurrentDictionary<long, ITcpClientSession> ActiveList
        {
            get
            {
                return activeDict;
            }
        }

        public ITcpClientSession Pop()
        {
            ITcpClientSession session;
            if (!pool.TryDequeue(out session))
            {
                if (Interlocked.Increment(ref count) <= maxSessions)
                {
                    session = new TcpClientSession(remoteEndPoint,bufferSize,loger);
                    session.Pool = this;
                    session.SessionId = sessionId.NewId();
                    session.PacketProtocol = GetProtocal();
                }
            }
            if (session != null)
                activeDict.TryAdd(session.SessionId, session);
            return session;
        }
        public virtual ITcpClientPacketProtocol GetProtocal()
        {
            return null;
        }
        public void Push(ITcpClientSession item)
        {
            if (activeDict.TryRemove(item.SessionId, out item))
                pool.Enqueue(item);
        }
    }
}
