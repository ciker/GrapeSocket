using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net;
using GrapeSocket.Client;
using GrapeSocket.Core.Interface;
using GrapeSocket.Client.Interface;
using GrapeSocket.Client.Protocol;
using System.Diagnostics;

namespace Echo.Client
{
    class MyClient : TcpClientSession
    {
        public MyClient(EndPoint server) : base(server, 1024)
        {

        }
        public override void OnReceived(ITcpClientSession session, IDynamicBuffer dataBuffer)
        {
            Received(session, dataBuffer);
        }

        public override ILoger GetLoger()
        {
            return new Loger();
        }

        public event EventHandler<IDynamicBuffer> Received;
    }
    class QueryClient : TcpClientSession
    {
        ConcurrentQueue<TaskCompletionSource<byte[]>> taskQueue = new ConcurrentQueue<TaskCompletionSource<byte[]>>();
        public QueryClient(EndPoint server) : base(server, 1024)
        {

        }
        public override void OnReceived(ITcpClientSession session, IDynamicBuffer dataBuffer)
        {
            byte[] result = new byte[dataBuffer.DataSize];
            Buffer.BlockCopy(dataBuffer.Buffer, 0, result, 0, dataBuffer.DataSize);
            TaskCompletionSource<byte[]> tSource;
            if (taskQueue.TryDequeue(out tSource))
                tSource.SetResult(result);
        }

        public async Task<byte[]> QueryAsync(byte[] data)
        {
           var tSource = new TaskCompletionSource<byte[]>();
            taskQueue.Enqueue(tSource);
            base.SendAsync(data);
            var cancelSource = new CancellationTokenSource(5000);
            tSource.Task.Wait(cancelSource.Token);
            return await tSource.Task;
        }
        public override void OnDisConnect(ITcpClientSession session)
        {
            Console.WriteLine("与服务器断开连接");
        }

        public override ILoger GetLoger()
        {
            return new Loger();
        }
    }
    public class Program
    {
        static MyClient client;
        static QueryClient QClient;
        static int receiveCount = 0;
        static int allCount = 100000;
        static Stopwatch sb = new Stopwatch();
        static void Main(string[] args)
        {
            AnswerTest();
        }
        public static void AnswerTest()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8088);
            QClient = new QueryClient(endPoint);
            QClient.PacketProtocol = new TcpClientPacketProtocol(1024, 1024 * 4);
            QClient.Connect();
            Console.WriteLine("连接服务器成功");
            var data = Encoding.UTF8.GetBytes("测试数据kjfl发送大法师大法是大法师大法是否阿斯发达说");
            while (true)
            {
                string c = Console.ReadLine();
                if (!string.IsNullOrEmpty(c))
                    allCount = int.Parse(c);
                Stopwatch sw = new Stopwatch();
                sw.Start();//开始记录时间
                var t1 = Task.Run(async () =>
                 {
                     for (var d = 0; d < allCount; d++)
                     {
                         var t = await QClient.QueryAsync(data);
                     }
                 });
                var t2 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t3 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t4 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t5 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t6 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t7 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t8 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t9 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                var t10 = Task.Run(async () =>
                {
                    for (var d = 0; d < allCount; d++)
                    {
                        var t = await QClient.QueryAsync(data);
                    }
                });
                Task.WaitAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
                sw.Stop();
                Console.WriteLine("{0}times a query is complated,The elapsed time:{1} seconds {2} milliseconds", allCount * 10, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
            }
        }
        public static void CommonTest()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8088);
            client = new MyClient(endPoint);
            client.PacketProtocol = new TcpClientPacketProtocol(1024, 1024 * 4);
            client.Received += ReceiveCommond;
            client.Connect();

            Stopwatch sw = new Stopwatch();
            Console.WriteLine("连接服务器成功");
            int i = 0;
            string c = Console.ReadLine();
            if (!string.IsNullOrEmpty(c))
                allCount = int.Parse(c);
            sw.Start();//开始记录时间
            sb.Start();
            while (i <= allCount)
            {
                i++;
                var data = Encoding.UTF8.GetBytes("My test" + i);
                client.SendAsync(data);
            }
            sw.Stop();
            Console.WriteLine("发送{0}次数据完成，运行时间：{1} 秒{2}毫秒", i, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
            Console.ReadLine();
        }
        public static ITcpClientPacketProtocol GetProtocol()
        {
            return new TcpClientPacketProtocol(1024, 1024 * 4);
        }
        public static void ReceiveCommond(object sender, IDynamicBuffer data)
        {
            if (Interlocked.Increment(ref receiveCount) >= allCount)
            {
                sb.Stop();
                Console.WriteLine("接收{0}次数据完成，运行时间：{1} 秒{2}毫秒", allCount, sb.Elapsed.Seconds, sb.Elapsed.Milliseconds);
            }
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
            Console.WriteLine(message);
        }

        public void Fatal(Exception e)
        {
            Console.WriteLine(e.Message);
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
