# GrapeSocket
A high performance socket component for .net core.

    public class MyServer : TcpServer
    {
        public MyServer(TcpServerConfig config, ILoger loger) : base(config, loger)
        { }
        static byte[] data = Encoding.UTF8.GetBytes("test data for server");
        static int count = 0;
        public override void OnReceived(ITcpSession session, IDynamicBuffer dataBuffer)
        {
            // var result = new byte[dataBuffer.DataSize];
            //Buffer.BlockCopy(dataBuffer.Buffer, 0, result, 0, dataBuffer.DataSize);
            //session.SessionData.Set("islogin", true);//login status
            //var txt= Encoding.UTF8.GetString(result);
            session.SendAsync(data);
        }
    }


    TcpServerConfig configOne = new TcpServerConfig { ServerId = 1, Name = "one", IP = "127.0.0.1", Port = 8088, BufferSize = 1024, MaxFixedBufferPoolSize = 1024 * 4, MaxConnections = 8000 };
    MyServer listener = new MyServer(configOne, loger);
    listener.Start();
