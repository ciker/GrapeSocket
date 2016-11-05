using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GrapeSocket.Server.Config;

namespace GrapeSocket.Server.Config
{
    public class TcpServerConfig
    {
        public uint ServerId { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// IP地址
        /// </summary>
        public string IP { get; set; }
        /// <summary>
        /// Ip端口
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 是否把网络字节顺序转为本地字节顺序
        /// </summary>
        public bool NetByteOrder { get; set; }
        /// <summary>
        /// 缓冲器数组大小
        /// </summary>
        public int BufferSize
        {
            get;
            set;
        }
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnections
        {
            get;
            set;
        }
        int backLog = 100;
        /// <summary>
        /// 监听器挂起队列的最大长度(默认100)
        /// </summary>
        public int BackLog { get { return backLog; } set { backLog = value; } }

        int socketTimeOut= 60 * 1000; //Socket超时设置为60秒
        /// <summary>
        /// 连接超时时间
        /// </summary>
        public int SocketTimeOut { get { return socketTimeOut; } set { socketTimeOut = value; } }
        int maxBufferPoolSize = 4 * 1024;
        /// <summary>
        /// buffer池最大量(一般用于合并分包)
        /// </summary>
        public int MaxFixedBufferPoolSize { get { return maxBufferPoolSize; }set { maxBufferPoolSize = value; } }
    }
}
