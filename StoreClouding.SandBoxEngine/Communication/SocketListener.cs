using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace StoreClouding.SandBoxEngine.Communication
{

    class SocketListener
    {
        public int Port { get; set; }
        public ProtocolType Protocol { get; set; }
        public string Host { get; set; }
        Socket listener = null;

        IPEndPoint localEndPoint;
        Thread listenerProcess = null;
        Thread pingProcess = null;
        int lastConnectionID = 0;
        bool connected = false;

        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public Dictionary<int, SocketListenerConnection> ActualConnections { get; internal set; }

        public delegate void MessageReceivedHandler(SocketListener listener, int connectionID, int messageTypeID, byte[] message);

        public delegate void CommonHandler(SocketListener listener, int connectionID);
        public event CommonHandler ConnectionAccepted;
        public event MessageReceivedHandler MessageReceived;
        public event CommonHandler MessageSended;
        public event CommonHandler ConnectionClosed;
        private List<int> pingResponse = null;
        bool acceptingPings = false;
        public SocketListener(string host,int port, ProtocolType protocol = ProtocolType.Tcp)
        {
            this.Port = port;
            this.Protocol = protocol;
            this.Host = host;
            IPHostEntry ipHostInfo = Dns.Resolve(host);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            localEndPoint = new IPEndPoint(ipAddress, port);

            listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
            ActualConnections = new Dictionary<int, SocketListenerConnection>();
        }

        public bool SendTo(int clientConnectionID, int messageTypeID, byte[] message)
        {

            try
            {
                if (!ActualConnections.ContainsKey(clientConnectionID))
                    return false;

                ActualConnections[clientConnectionID].Send(messageTypeID, message);
            }
            catch (Exception) { }

            return true;
        }

        public void Kick(int connectionID)
        {
            CloseConnection(connectionID, true);
        }

        public void KickAll()
        {
            int[] kickList;

            kickList = ActualConnections.Select(client => client.Value.ConnectionID).ToArray();

            foreach (var id in kickList)
                Kick(id);
        }

        public void SendToAll(int messageTypeID, byte[] message)
        {

            foreach (var connection in ActualConnections)
            {
                try
                {
                    connection.Value.Send(messageTypeID, message);
                }
                catch (Exception) { }
            }

        }

        public bool Close()
        {
            lock (listener)
            {
                if (!connected)
                    return false;

                connected = false;
                listenerProcess = null;
                pingProcess = null;

                KickAll();

                if(ActualConnections.Count > 0)
                    Thread.Sleep(5000);//aguarda kick geral

                lock (listener)
                {
                    ActualConnections.Clear();
                    listener.Close();
                }
                return true;
            }

        }
        public bool Start()
        {
            if (connected)
                return false;

            lock (listener)
            {
                //bind and list max connections
                listener.Bind(localEndPoint);
                //TODO: definnir maximo de conexões
                listener.Listen(int.MaxValue);
            }
            connected = true;
            listenerProcess = new Thread(() =>
            {
                while (connected)
                {

                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    listener.BeginAccept(
                        new AsyncCallback(NewConnection),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();


                }
            });

            pingProcess = new Thread(() =>
            {
                Thread.Sleep(5000);//aguarda antes de começar os pings

                while (connected)
                {
                    acceptingPings = true;

                    pingResponse = new List<int>(ActualConnections.Count);
                    //executa novo ping
                    foreach (var connection in ActualConnections)
                        connection.Value.Send((int)Common.SocketMessageTypes.Ping, Common.SocketDefaultMessages.Ping);

                    Thread.Sleep(5000);//aguarda time out do ping
                    acceptingPings = false;

                    List<SocketListenerConnection> connectionsToClose;

                    connectionsToClose = ActualConnections.Where(client => !pingResponse.Contains(client.Key)).Select(client => client.Value).ToList();

                    //encerra a conexões que não conseguiram o ping a tempo
                    foreach (var connection in connectionsToClose)
                        CloseConnection(connection.ConnectionID, false);

                    Thread.Sleep(5000);//aguarda tempo para proximo ping
                }
            });

            listenerProcess.Start();
            //pingProcess.Start();
            return true;
        }

        void NewConnection(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();
            try
            {
                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;

                Socket handler = listener.EndAccept(ar);
                //connection id 
                lastConnectionID++;
                int id = lastConnectionID;
                SocketListenerConnection connection = new SocketListenerConnection(id, handler);

                connection.ClientMessageReceived += (client, messageID, data) =>
                {
                    if (messageID == (int)Common.SocketMessageTypes.Ping)
                    {
                        if (acceptingPings)
                            pingResponse.Add(client.ConnectionID);
                    }
                    else if(MessageReceived != null)
                    {
                         MessageReceived(this, client.ConnectionID, messageID, data);
                    }
 
                };

                connection.SendMessageCompleted += (client) =>
                {
                    if (MessageSended != null)
                        MessageSended(this, client.ConnectionID);
                };

                connection.ConnectionClosed += (client) =>
                {
                    CloseConnection(client.ConnectionID, false);
                };

                ActualConnections.Add(id, connection);
                if (ConnectionAccepted != null)
                    ConnectionAccepted(this, id);
            }
            catch (Exception) { }

        }
        void CloseConnection(int clientID, bool sendMessage)
        {
            lock (ActualConnections)
            {

                try
                {
                    if (ActualConnections[clientID].Connected)
                    {     //desconecta
                        ActualConnections[clientID].Close(sendMessage);

                        if (ConnectionClosed != null)
                            ConnectionClosed(this, clientID);
                    }
                }
                catch (Exception) { }

                try
                {
                    //remove da lista de conexões
                    ActualConnections.Remove(clientID);
                }
                catch (Exception) { }


            }
        }


    }
}
