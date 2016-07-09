using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GrapeSocket.Core
{
    public class PackageId
    {
        private static uint timeMask = 0xffffc000;
        private static uint idMask = 0x3fff;
        private int id = 0;
        public uint NewId()
        {
            int newId = Interlocked.Increment(ref id);
            int time = int.Parse(DateTime.Now.ToString("HHmmss"));
            long result = ((time << 14) & timeMask) | (newId & idMask);
            return (uint)result;
        }
    }
}
