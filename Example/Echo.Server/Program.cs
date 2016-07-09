using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GrapeSocket.Server.Config;
using GrapeSocket.Core.Interface;
using GrapeSocket.Server.Interface;
using GrapeSocket.Server;

namespace Echo.Server
{
    public class MyServer : TcpServer
    {
        public MyServer(TcpServerConfig config, ILoger loger) : base(config, loger)
        { }
        static byte[] data = Encoding.UTF8.GetBytes("测试数据服务器返回");
        static int count = 0;
        public override void OnReceived(ITcpSession session, IDynamicBuffer dataBuffer)
        {
            // var result = new byte[dataBuffer.DataSize];
            //Buffer.BlockCopy(dataBuffer.Buffer, 0, result, 0, dataBuffer.DataSize);
            //session.SessionData.Set("islogin", true);//设置登录状态
            //var txt= Encoding.UTF8.GetString(result);
            session.SendAsync(data);
        }
    }
    public class MyMonitor : TcpMonitor
    {
        int loginTimeOutMilliseconds;
        public MyMonitor(MonitorConfig config, int loginTimeOutMilliseconds) : base(config)
        {
            this.loginTimeOutMilliseconds = loginTimeOutMilliseconds;
        }
        public override async Task Start()
        {
            LoginMonitor();
            await base.Start();
        }
        public async Task LoginMonitor()
        {
            while (true)
            {
                await Task.Delay(loginTimeOutMilliseconds);
                foreach (var server in ServerList)
                {
                    List<ITcpSession> clearList = new List<ITcpSession>();
                    foreach (var sessionKV in server.OnlineList)
                    {
                        var session = sessionKV.Value;
                        var isLogin = session.SessionData.Get("islogin");
                        if ((DateTime.Now - session.ActiveDateTime).TotalMilliseconds > loginTimeOutMilliseconds && isLogin == null)
                        {
                            clearList.Add(session);
                        }
                    }
                    foreach (var session in clearList)
                    {
                        session.DisConnect();
                    }
                }
            }
        }
    }
    public class Program
    {
        static List<string> li = new List<string>();
        static Loger loger = new Loger();
        static void Main(string[] args)
        {
            TcpServerConfig configOne = new TcpServerConfig { ServerId = 1, Name = "one", IP = "127.0.0.1", Port = 8088, BufferSize = 1024, MaxFixedBufferPoolSize = 1024 * 4, MaxConnections = 8000 };
            MyServer listener = new MyServer(configOne, loger);
            listener.Start();
           // MonitorConfig monitorConfig = new MonitorConfig();
           // monitorConfig.WorkDelayMilliseconds = 10000;
           // monitorConfig.TimeoutMilliseconds = 10000;
           // MyMonitor monitor = new MyMonitor(monitorConfig, 3000);
           // monitor.AddServer(listener);
           // monitor.Start();
            Console.WriteLine("服务器已启动");
            Console.ReadLine();
        }
    }
    public class Loger : ILoger
    {
        public void Debug(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void Debug(string message)
        {
            throw new NotImplementedException();
        }

        public void Error(Exception e)
        {
            throw new NotImplementedException();
        }

        public void Error(string message)
        {
            throw new NotImplementedException();
        }

        public void Fatal(Exception e)
        {
            throw new NotImplementedException();
        }

        public void Fatal(string message)
        {
            throw new NotImplementedException();
        }

        public void Info(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void Info(string message)
        {
            throw new NotImplementedException();
        }

        public void Log(string message)
        {
            throw new NotImplementedException();
        }

        public void Trace(Exception exception)
        {
            throw new NotImplementedException();
        }

        public void Trace(string message)
        {
            throw new NotImplementedException();
        }

        public void Warning(Exception e)
        {
            throw new NotImplementedException();
        }

        public void Warning(string message)
        {
            throw new NotImplementedException();
        }
    }
}
