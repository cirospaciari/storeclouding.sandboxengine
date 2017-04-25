using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
namespace StoreClouding.SandBoxEngine.Communication
{
    class SocketListenerConnection
    {
        //8K buffer (min 12 bytes for headers)
        public const int BufferSize = 8192;

        public delegate void ClientMessageReceivedHandler(SocketListenerConnection connection, int messageTypeID, byte[] message);
        public delegate void CommonHandler(SocketListenerConnection connection);
        public event ClientMessageReceivedHandler ClientMessageReceived;
        public event CommonHandler SendMessageCompleted;
        public event CommonHandler ConnectionClosed;
        public Socket ClientHandler { get; internal set; }
        public int ConnectionID { get; internal set; }

        byte[] buffer = new byte[BufferSize];

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
        public SocketListenerConnection(int clientID, Socket clientHandler)
        {
            this.ConnectionID = clientID;
            ClientHandler = clientHandler;
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
                    if (bytesRead < 12)
                    {
                        //mensagem inválida
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
                    if (bytesRead > 12)
                    {
                        readingData.AddRange(data.Skip(12));
                        if (readingData.Count == messageSize )
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
                    }//invalid command
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
            if (readingMessageTypeID == (int)Common.SocketMessageTypes.Disconnected)
            {
                Close(true);
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
                for (int i = 0; i < fullMessage.Count; i++)
                    checkSum = checkSum ^ fullMessage[i];
            }
            fullMessage.InsertRange(0, BitConverter.GetBytes(checkSum));
            var byteData = fullMessage.ToArray();

            ClientHandler.Send(byteData);

            if (SendMessageCompleted != null && messageTypeID != (int)Common.SocketMessageTypes.Ping)
                SendMessageCompleted.Invoke(this);
        }

        public void Close(bool sendMessage)
        {
            lock (ClientHandler)
            {
                try
                {
                    if (sendMessage)
                    {
                        byte[] messageType = BitConverter.GetBytes((int)Common.SocketMessageTypes.Disconnected);
                        byte[] messageSize = BitConverter.GetBytes(Common.SocketDefaultMessages.Disconnected.Length);

                        var closeMessage = new List<byte>();
                        closeMessage.AddRange(messageType);
                        closeMessage.AddRange(messageSize);
                        closeMessage.AddRange(Common.SocketDefaultMessages.Disconnected);

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
                    }
                }
                catch (Exception) { }

                if (ConnectionClosed != null)
                    ConnectionClosed.Invoke(this);
            }
        }

        ~SocketListenerConnection()
        {
            try
            {
                //confirm close on destroy
                this.Close(false);
            }
            catch (Exception) { }
        }
    }
}
