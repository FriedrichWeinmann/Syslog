using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Syslog
{
    public abstract class WorkerBase
    {
        public ConcurrentQueue<string> ProcQueue = new ConcurrentQueue<string>();

        public string Server;
        public int Port;
        public Server ServerObject;

        public Task Task;

        public TcpClient Client
        {
            get
            {
                if (_Client == null)
                {
                    _Client = new TcpClient();
                    _Client.Connect(Server, Port);
                }

                if (!_Client.Connected)
                {
                    _Client.Dispose();
                    _Client = new TcpClient();
                    _Client.Connect(Server, Port);
                }

                return _Client;
            }
        }
        private TcpClient _Client;


        public WorkerBase(string Server, int Port, Server ServerObject)
        {
            this.Server = Server;
            this.Port = Port;
            
            ServerObject.Workers.Add(this);
        }

        public void Send(string Message)
        {
            List<byte> bytes = new List<byte>(Encoding.ASCII.GetBytes(Message));
            if (bytes[bytes.Count - 1] != 0)
                bytes.Add(0);
            Client.Client.Send(bytes.ToArray());
        }

        public void Execute()
        {
            string line;
            while (true)
            {
                if (!ProcQueue.TryDequeue(out line))
                {
                    System.Threading.Thread.Sleep(250);
                    continue;
                }

                if (String.IsNullOrEmpty(line))
                    continue;

                string converted;
                try { converted = Convert(line); }
                catch (Exception e)
                {
                    ServerObject.ErrorQueue.Enqueue($"Error converting message { line } : { e.Message }");
                    continue;
                }

                try { Send(converted); }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                    try { Send(converted); }
                    catch
                    {
                        System.Threading.Thread.Sleep(100);
                        try { Send(converted); }
                        catch (Exception e)
                        {
                            ServerObject.ErrorQueue.Enqueue($"Error sending message { line } to destination : { e.Message }");
                        }
                    }
                }
            }
        }

        public abstract string Convert(string Line);

        public void Start()
        {
            Task = new Task(() => Execute());
            Task.Start();
        }
    }
}
