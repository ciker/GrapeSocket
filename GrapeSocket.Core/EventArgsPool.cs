using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using GrapeSocket.Core.Interface;
using System.Net.Sockets;

namespace GrapeSocket.Core
{
    public class EventArgsPool : IPool<SocketAsyncEventArgs>
    {
        private ConcurrentStack<SocketAsyncEventArgs> pool = new ConcurrentStack<SocketAsyncEventArgs>();
        int maxCount,allCount=0;
        public EventArgsPool(int maxCount)
        {
            this.maxCount = maxCount;
        }
        public int Count
        {
            get
            {
                return allCount;
            }
        }

        public int FreeCount
        {
            get
            {
                return pool.Count();
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs result;
            if (!pool.TryPop(out result))
            {
                if (Interlocked.Increment(ref allCount) <= maxCount)
                {
                    result = new SocketAsyncEventArgs();
                }
            }
            return result;
        }

        public void Push(SocketAsyncEventArgs item)
        {
            pool.Push(item);
        }
    }
}
