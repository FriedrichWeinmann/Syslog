using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Syslog
{
    public class Server
    {
        internal List<WorkerBase> Workers = new List<WorkerBase>();
        internal ConcurrentQueue<string> ErrorQueue = new ConcurrentQueue<string>();
        internal ConcurrentQueue<string> InQueue = new ConcurrentQueue<string>();

        internal List<Connection> Sockets = new List<Connection>();
        internal ConcurrentDictionary<Guid, Connection> DataPending = new ConcurrentDictionary<Guid, Connection>();
        public Socket ServerSocket { get; private set; }
        public int BufferSize = 1048576;

        #region Parameter-Backed Properties
        public int WorkerCount { get; private set; }
        public int InPort { get; private set; }
        public IPAddress ListenOn { get; private set; }
        public int OutPort { get; private set; }
        public string OutServer { get; private set; }
        public IDictionary<string, object> Parameters { get; private set; }
        public WorkerKind Kind { get; private set; }
        #endregion Parameter-Backed Properties

        public string[] Errors
        {
            get
            {
                return ErrorQueue.ToArray();
            }
        }
        public void ClearErrors()
        {
            ErrorQueue = new ConcurrentQueue<string>();
        }
        public ServerState State
        {
            get
            {
                if (ServerSocket == null)
                    return ServerState.Stopped;
                return ServerState.Running;
            }
        }

        public Server(int WorkerCount, int InPort, IPAddress ListenOn, int OutPort, string OutServer, WorkerKind Kind, IDictionary<string, object> Parameters)
        {
            this.WorkerCount = WorkerCount;
            this.InPort = InPort;
            this.ListenOn = ListenOn;
            this.OutPort = OutPort;
            this.OutServer = OutServer;
            this.Parameters = Parameters;
            this.Kind = Kind;
        }

        public void Start()
        {
            IPEndPoint serverEndPoint;
            try { serverEndPoint = new IPEndPoint(ListenOn, InPort); }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException("Port number entered would seem to be invalid, should be between 1024 and 65000", e);
            }

            try { ServerSocket = new Socket(serverEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp); }
            catch (SocketException e) { throw new ApplicationException("Could not create socket, check to make sure not duplicating port", e); }

            try
            {
                ServerSocket.Bind(serverEndPoint);
                ServerSocket.Listen(65535);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"Error occured while binding socket: {e.Message}", e);
            }

            try { ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket); }
            catch (Exception e)
            {
                throw new ApplicationException($"Error occured starting listeners: {e.Message}", e);
            }

            for (int index = 0; index < WorkerCount; index++)
            {
                switch (Kind)
                {
                    case WorkerKind.Regex:
                        new WorkerRegex(OutServer, OutPort, this).Start();
                        break;
                }
            }

            // Start the worker threads that transfer incoming data to the worker agents
            StartDispatch();
        }

        public void Stop()
        {
            if (ServerSocket == null)
                return;

            foreach (Connection connection in Sockets)
                connection.Disconnect();

            try { ServerSocket.Disconnect(false); }
            catch { }
            
            ServerSocket.Dispose();
            ServerSocket = null;
        }

        public List<string> ReadAny()
        {
            Random random = new Random();
            return Sockets[random.Next(Sockets.Count)].Read();
        }

        private int _LastReadIndex = -1;
        /// <summary>
        /// Reads strings from any active connection.
        /// Uses a rough Round-Robin method of selecting which connection to read from.
        /// </summary>
        public List<string> ReadOne()
        {
            Connection[] connections = DataPending.Values.ToArray();
            List<string> data;

            int startIndex = _LastReadIndex + 1;
            if (startIndex >= connections.Length)
                startIndex = 0;
            int index = startIndex;

            while (index < connections.Length)
            {
                data = connections[index].Read();
                if (data != null && data.Count > 0)
                {
                    _LastReadIndex = index;
                    return data;
                }
                index++;
            }

            index = 0;

            while (index < startIndex)
            {
                data = connections[index].Read();
                if (data != null && data.Count > 0)
                {
                    _LastReadIndex = index;
                    return data;
                }
                index++;
            }
            return null;
        }

        public List<string> ReadAll()
        {
            List<string> results = new List<string>();
            foreach (Connection connection in DataPending.Values)
                results.AddRange(connection.ReadAll());
            return results;
        }

        #region Async Operations
        private void AcceptCallback(IAsyncResult result)
        {
            Connection connection = new Connection();
            try
            {
                // Finish accepting the connection
                Socket socket = (Socket)result.AsyncState;
                connection = new Connection();
                connection.Socket = socket.EndAccept(result);
                connection.Buffer = new byte[BufferSize];
                connection.Server = this;
                lock (Sockets)
                {
                    Sockets.Add(connection);
                }
                DataPending[connection.Id] = connection;

                // Queue recieving of data from the connection
                connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), connection);
                // Queue the accept of the next incomming connection
                ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);
            }
            catch
            {
                connection.Disconnect();
                // Queue the next accept, think this should be here, stop attacks based on killing the waiting listeners
                ServerSocket.BeginAccept(new AsyncCallback(AcceptCallback), ServerSocket);
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            // get our connection from the callback
            Connection connection = (Connection)result.AsyncState;
            // catch any errors, we'd better not have any
            try
            {
                //Grab our buffer and count the number of bytes receives
                int bytesRead = connection.Socket.EndReceive(result);
                //make sure we've read something, if we haven't it supposadly means that the client disconnected
                if (bytesRead > 0)
                {
                    //put whatever you want to do when you receive data here
                    byte[] tempData = new byte[bytesRead];
                    Array.Copy(connection.Buffer, 0, tempData, 0, bytesRead);
                    connection.DataReceived.Enqueue(new DataPackage(tempData));

                    //Queue the next receive
                    connection.Socket.BeginReceive(connection.Buffer, 0, connection.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), connection);
                }
                else
                {
                    // Callback run but no data, close the connection
                    // supposadly means a disconnect
                    // and we still have to close the socket, even though we throw the event later
                    connection.Disconnect();
                }
            }
            catch
            {
                // Something went terribly wrong
                // which shouldn't have happened
                connection.Disconnect();
            }
        }
        #endregion Async Operations

        #region Dispatcher
        private void Dispatch()
        {
            while (Workers.Count <= 0)
            {
                System.Threading.Thread.Sleep(100);
            }
            int lastIndex = -1;
            string line;
            while (true)
            {
                if (!InQueue.TryDequeue(out line))
                {
                    System.Threading.Thread.Sleep(250);
                    continue;
                }

                lastIndex++;
                if (lastIndex >= Workers.Count)
                    lastIndex = 0;

                Workers[lastIndex].ProcQueue.Enqueue(line);
            }
        }

        private void Receive()
        {
            List<string> received;
            while (true)
            {
                received = ReadOne();
                if (received.Count == 0)
                {
                    System.Threading.Thread.Sleep(250);
                    continue;
                }

                foreach (string message in received)
                {
                    if (String.IsNullOrEmpty(message))
                        continue;
                    InQueue.Enqueue(message);
                }
            }
        }

        public void StartDispatch()
        {
            new Task(() => Dispatch()).Start();
            new Task(() => Receive()).Start();
        }
        #endregion Dispatcher
    }
}
