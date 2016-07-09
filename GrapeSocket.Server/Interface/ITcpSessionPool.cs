using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrapeSocket.Server.Session;
using GrapeSocket.Core.Interface;

namespace GrapeSocket.Server.Interface
{
    public interface ITcpSessionPool:IMonitorPool<uint, ITcpSession>
    {
        /// <summary>
        /// 内存池
        /// </summary>
        IPool<IFixedBuffer> FixedBufferPool { get; set; }
        /// <summary>
        /// 池所属的Server
        /// </summary>
        ITcpServer TcpServer { get; set; }
    }
}
