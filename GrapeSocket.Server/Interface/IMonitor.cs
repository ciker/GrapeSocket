using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrapeSocket.Server.Interface
{
    /// <summary>
    /// 监控器
    /// </summary>
    public interface IMonitor
    {
        void AddServer(ITcpServer Server);
        Task Start();
        void Stop();
    }
}
