using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrapeSocket.Core.Interface
{
    public interface IMonitorPool<K,T>:IPool<T>
    {
        /// <summary>
        /// 活动列表
        /// </summary>
        ConcurrentDictionary<K, T> ActiveList { get; }
    }
}
