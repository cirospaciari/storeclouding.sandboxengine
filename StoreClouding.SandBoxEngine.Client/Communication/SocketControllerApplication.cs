using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Communication
{
    public class SocketControllerApplication : IApplication
    {
        //por enquanto trabalha com apenas uma conexão mas futuro irá trabalhar com varias e de varios tipos
        private SocketClient client;
        private string Host;
        private int Port;
        private ProtocolType Type;
        private ConcurrentDictionary<int, SocketMessageTypeBase> Actions = new ConcurrentDictionary<int, SocketMessageTypeBase>();

        public SocketControllerApplication(string host, int port, ProtocolType type = ProtocolType.Tcp)
        {
            this.Host = host;
            this.Port = port;
            this.Type = type;
        }

        void MessageReceived(SocketClient client, int messageTypeID, byte[] message)
        {
            SocketMessageTypeBase action;
            //action inválida então desconecta cliente
            if (!Actions.TryGetValue(messageTypeID, out action))
            {
                client.Close();
                return;
            }
            action.OnMessageReceived(message);
        }

        public bool RegisterMessageType(SocketMessageTypeBase message)
        {
            if (message == null)
                return false;
            message.Controller = this;
            return Actions.TryAdd(message.MessageTypeID, message);
        }

        public SocketMessageTypeBase GetMessageType(int typeID)
        {
            SocketMessageTypeBase message;
            if (!Actions.TryGetValue(typeID, out message))
                return null;
            return message;
        }
        #region No futuro usará varias conexões
        public void Send(int messageTypeID, byte[] message)
        {
            client.Send(messageTypeID, message);
        }

        #endregion

        public bool Start(out string error)
        {
            try
            {
                client = new SocketClient(Host, Port, Type);
                client.ClientMessageReceived += MessageReceived;
                error = null;
                return true;
            }
            catch (Exception)
            {
                error = "Failed to open connection to the server";
                return false;
            }
        }

        public bool Update(out string error)
        {
            error = null;
            SocketMessageTypeBase[] actionList = null;
            lock (Actions)
            {
                actionList = Actions.Values.ToArray();
            }
            if (actionList != null)
                foreach (var action in actionList)
                {

                    if (!action.Update(out error))
                        return false;

                }


            return true;
        }

        public bool Stop(out string error)
        {
            try
            {
                client.Close();
                error = null;
                return true;
            }
            catch (Exception)
            {
                error = "Failed to close connection to the server";
                return false;
            }
        }
    }
}
