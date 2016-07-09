using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrapeSocket.Core.Interface;

namespace GrapeSocket.Client.Interface
{
    public interface ITcpClientSessionPool : IMonitorPool<long, ITcpClientSession>
    {
        ITcpClientPacketProtocol GetProtocal();
    }
}
