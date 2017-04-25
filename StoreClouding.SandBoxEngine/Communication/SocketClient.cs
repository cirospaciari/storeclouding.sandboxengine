using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace StoreClouding.SandBoxEngine.Communication
{
    class SocketClient
    {
        // The port number for the remote device.
        public int Port { get; internal set; }
        public string Host { get; internal set; }

        public delegate void ClientMessageReceivedHandler(SocketClient client, int messageTypeID, byte[] message);
        public delegate void CommonHandler(SocketClient client);
        public event CommonHandler SendMessageCompleted;
        public event CommonHandler ConnectionClosed;
        public event ClientMessageReceivedHandler ClientMessageReceived;



        private Socket ClientHandler;
        private byte[] buffer = new byte[SocketListenerConnection.BufferSize];
        List<byte> readingData = null;
        int readingMessageTypeID = -1;
        int messageSize = -1;
        int messageCheckSum = -1;
        int receivedCheckSum = -1;
        public bool Connected
        {
            get
            {
                return ClientHandler.Connected;
            }
        }
        public SocketClient(string host, int port, ProtocolType protocol = ProtocolType.Tcp)
        {
            IPHostEntry ipHostInfo = Dns.Resolve(host);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            ClientHandler = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            ClientHandler.Connect(remoteEP);

            GetNewMessage();
        }
        void GetMoreData()
        {
            ClientHandler.BeginReceive(buffer, 0, SocketListenerConnection.BufferSize, 0,
            new AsyncCallback(ReadCallback), this);
        }
        void GetNewMessage()
        {
            readingData = null;
            ClientHandler.BeginReceive(buffer, 0, SocketListenerConnection.BufferSize, 0,
            new AsyncCallback(ReadCallback), this);
        }
        void ReadCallback(IAsyncResult ar)
        {
            if (!this.ClientHandler.Connected)
            {
                Close(false);
                return;
            }

            if (this.ClientMessageReceived == null)
            {
                GetNewMessage();
                return;
            }

            int bytesRead = this.ClientHandler.EndReceive(ar);
            if (bytesRead > 0)
            {
                List<byte> data = buffer.Take(bytesRead).ToList();
                if (readingData == null)
                {
                    if (bytesRead < 8)
                    {
                        GetNewMessage();
                        return;
                    }
                    try
                    {

                        messageCheckSum = BitConverter.ToInt32(data.Take(4).ToArray(), 0);
                        readingMessageTypeID = BitConverter.ToInt32(data.Skip(4).Take(4).ToArray(), 0);
                        messageSize = BitConverter.ToInt32(data.Skip(8).Take(4).ToArray(), 0);
                        receivedCheckSum = 0;
                        //checksum
                        unchecked
                        {
                            for (int i = 4; i < data.Count; i++)
                                receivedCheckSum = receivedCheckSum ^ data[i];
                        }
                    }
                    catch (Exception)
                    {
                        readingMessageTypeID = -1;
                        messageSize = 0;
                    }

                    if (readingMessageTypeID <= -1 || messageSize <= 0)
                    {
                        GetNewMessage();
                        return;
                    }
                    readingData = new List<byte>();
                    if (bytesRead > 8)
                    {
                        readingData.AddRange(data.Skip(8));
                        if (readingData.Count == messageSize)
                        {
                            if (messageCheckSum != receivedCheckSum)
                            {
                                //mensagem inválida ignora
                                GetNewMessage();
                                return;
                            }
                            MessageReceived(readingMessageTypeID, readingData.ToArray());
                            return;
                        }//invalid command
                        else if (readingData.Count > messageSize)
                        {

                            GetNewMessage();
                            return;
                        }
                    }
                    GetMoreData();

                }
                else
                {
                    //checksum
                    unchecked
                    {
                        for (int i = 4; i < data.Count; i++)
                            receivedCheckSum = receivedCheckSum ^ data[i];
                    }
                    readingData.AddRange(data);
                    if (readingData.Count == messageSize)
                    {
                        if (messageCheckSum != receivedCheckSum)
                        {
                            //mensagem inválida ignora
                            GetNewMessage();
                            return;
                        }
                        MessageReceived(readingMessageTypeID, readingData.ToArray());
                        return;
                    }
                    //invalid command
                    else if (readingData.Count > messageSize)
                    {

                        GetNewMessage();
                        return;
                    }
                    GetMoreData();
                }
            }
        }

        void MessageReceived(int readingMessageTypeID, byte[] data)
        {
            if (readingMessageTypeID == SocketListener.DisconnectedMessageID)
            {
                Close(false);
                return;
            }

            //if ping send pong
            if (readingMessageTypeID == SocketListener.PingMessageID)
            {
                Send(SocketListener.PingMessageID, SocketListener.PingMessage);
                GetNewMessage();
                return;
            }

            if (this.ClientMessageReceived != null)
                this.ClientMessageReceived.Invoke(this, readingMessageTypeID, data);

            GetNewMessage();
        }

        public void Send(int messageTypeID, byte[] message)
        {
            if (!this.ClientHandler.Connected)
            {
                Close(false);
                return;
            }
            byte[] messageType = BitConverter.GetBytes(messageTypeID);
            byte[] messageSize = BitConverter.GetBytes(message.Length);

            var fullMessage = new List<byte>();
            fullMessage.AddRange(messageType);
            fullMessage.AddRange(messageSize);
            fullMessage.AddRange(message);

            int checkSum = 0;
            //checksum
            unchecked
            {
                for (int i = 4; i < fullMessage.Count; i++)
                    checkSum = checkSum ^ fullMessage[i];
            }
            fullMessage.InsertRange(0, BitConverter.GetBytes(checkSum));

            var byteData = fullMessage.ToArray();

            //send precisa ser sincrono para garantir que não vai ter merda depois
            ClientHandler.Send(byteData);

            if (SendMessageCompleted != null && (messageTypeID != SocketListener.PingMessageID))
                SendMessageCompleted.Invoke(this);

        }

        public void Close(bool sendMessage = true)
        {
            lock (ClientHandler)
            {
                try
                {
                    if (sendMessage)
                    {
                        byte[] messageType = BitConverter.GetBytes((int)Common.SocketMessageTypes.Disconnected);
                        byte[] messageSize = BitConverter.GetBytes(SocketListener.DisconnectedMessage.Length);

                        var closeMessage = new List<byte>();
                        closeMessage.AddRange(messageType);
                        closeMessage.AddRange(messageSize);
                        closeMessage.AddRange(SocketListener.DisconnectedMessage);

                        ClientHandler.Send(closeMessage.ToArray());
                    }
                }
                catch (Exception) { }

                try
                {
                    if (ClientHandler.Connected)
                    {
                        ClientHandler.Shutdown(SocketShutdown.Both);
                        ClientHandler.Close();
                        if (ConnectionClosed != null)
                            ConnectionClosed(this);
                    }
                }
                catch (Exception) { }

            }
        }
        ~SocketClient()
        {
            try
            {
                Close(false);
            }
            catch (Exception) { }
        }
    }
}
