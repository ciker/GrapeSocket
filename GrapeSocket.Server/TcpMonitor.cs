using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GrapeSocket.Server.Interface;
using GrapeSocket.Core.Interface;
using GrapeSocket.Server.Config;

namespace GrapeSocket.Server
{
    public class TcpMonitor : IMonitor
    {
        protected List<ITcpServer> ServerList = new List<ITcpServer>();
        protected MonitorConfig config;
        protected CancellationTokenSource token = new CancellationTokenSource();
        public TcpMonitor(MonitorConfig config)
        {
            this.config = config;
        }
        public void AddServer(ITcpServer Server)
        {
            ServerList.Add(Server);
        }

        public virtual async Task Start()
        {
            await Task.Factory.StartNew(StartAsync, token.Token);
        }
        private async Task StartAsync()
        {
            while (true)
            {
                await Task.Delay(config.WorkDelayMilliseconds);
                foreach (var server in ServerList)
                {
                    List<ITcpSession> clearList = new List<ITcpSession>();
                    foreach (var sessionKV in server.OnlineList)
                    {
                        var session = sessionKV.Value;
                        if ((DateTime.Now - session.ActiveDateTime).TotalMilliseconds > config.TimeoutMilliseconds)
                        {
                            clearList.Add(session);
                        }
                    }
                    foreach (var session in clearList)
                    {
                        if (OnTimeOut(session))
                            session.DisConnect();
                    }
                }
            }
        }
        public virtual bool OnTimeOut(ITcpSession session)
        {
            return true;
        }
        public void Stop()
        {
            token.Cancel();
        }
    }
}
