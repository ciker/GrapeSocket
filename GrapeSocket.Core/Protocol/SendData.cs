using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrapeSocket.Core.Protocol
{
    public class SendData
    {
        public byte[] Data { get; set; }
        public int Offset { get; set; }
    }
}
