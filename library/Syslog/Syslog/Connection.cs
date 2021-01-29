using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Syslog
{
    public class Connection
    {
        public Server Server;
        public Socket Socket;
        public byte[] Buffer;
        public ConcurrentQueue<DataPackage> DataReceived = new ConcurrentQueue<DataPackage>();
        public Guid Id;
        public bool Disconnected = false;
        public bool SplitOnLessThan = false;

        public Connection()
        {
            Id = Guid.NewGuid();
        }

        private List<byte> _CurrentMessage = new List<byte>();

        public List<string> Read()
        {
            List<string> result = new List<string>();

            DataPackage tempData;
            bool success = DataReceived.TryDequeue(out tempData);
            if (!success)
                return null;

            int index = 0;
            while (true)
            {
                if (index >= tempData.Data.Length)
                    break;

                // Null-terminated text
                if (tempData.Data[index] == 0)
                {
                    index++;
                    if (_CurrentMessage.Count > 0)
                        result.Add(Encoding.ASCII.GetString(_CurrentMessage.ToArray()));
                    _CurrentMessage = new List<byte>();
                    continue;
                }
                // The character "<" happens, which is always the first character and cannot happen later on
                if (SplitOnLessThan && tempData.Data[index] == 60 && _CurrentMessage.Count > 0)
                {
                    result.Add(Encoding.ASCII.GetString(_CurrentMessage.ToArray()));
                    _CurrentMessage = new List<byte>();
                }

                _CurrentMessage.Add(tempData.Data[index]);
                index++;
            }

            if (Disconnected && DataReceived.Count == 0 && Server.DataPending.ContainsKey(Id))
            {
                Connection temp;
                Server.DataPending.TryRemove(Id, out temp);
            }

            return result;
        }

        public List<string> ReadAll()
        {
            List<string> results = new List<string>();
            List<string> temp;
            while (true)
            {
                temp = Read();
                if (temp == null)
                    break;
                results.AddRange(temp);
            }
            return results;
        }

        public void Disconnect()
        {
            if (Socket != null)
            {
                Socket.Close();
                lock (Server.Sockets)
                {
                    Server.Sockets.Remove(this);
                }
            }
            Disconnected = true;
        }
    }
}
