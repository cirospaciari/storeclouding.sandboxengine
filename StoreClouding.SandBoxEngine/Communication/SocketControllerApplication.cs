using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace StoreClouding.SandBoxEngine.Communication
{
    public class SocketControllerApplication : IApplication
    {
        //por enquanto trabalha com apenas uma conexão mas futuro irá trabalhar com varias e de varios tipos
        private SocketListener listener;
        private ConcurrentDictionary<int, SocketMessageTypeBase> Actions = new ConcurrentDictionary<int, SocketMessageTypeBase>();

        public SocketControllerApplication(string host, int port, ProtocolType type = ProtocolType.Tcp)
        {
            listener = new SocketListener(host, port, type);
            listener.MessageReceived += MessageReceived;
        }

        void MessageReceived(SocketListener listener, int connectionID, int messageTypeID, byte[] message)
        {
            SocketMessageTypeBase action;
            //action inválida então desconecta cliente
            if (!Actions.TryGetValue(messageTypeID, out action))
            {
                listener.Kick(connectionID);
                return;
            }
            // no futuro connectionID será um ID unico pra todas as conexões de um mesmo usuário
            action.OnMessageReceived(connectionID, message);
        }

        public bool RegisterMessageType(SocketMessageTypeBase message)
        {
            if (message == null)
                return false;
            message.Controller = this;
            return Actions.TryAdd(message.MessageTypeID, message);
        }

        #region No futuro usará varias conexões e o connectionID será um  generico que apenas o controller entenda
        public void Send(int connectionID, int messageTypeID, byte[] message)
        {
            listener.SendTo(connectionID, messageTypeID, message);
        }

        public void Kick(int connectionID)
        {
            listener.Kick(connectionID);
        }

        public void KickAll()
        {
            listener.KickAll();
        }

        public void SendToAll(int messageTypeID, byte[] message)
        {
            listener.SendToAll(messageTypeID, message);
        }

        #endregion

        public bool Start(out string error)
        {
            try
            {
                if (!listener.Start())
                {
                    error = "Failed to connect listener";
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
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
                if (!listener.Close())
                {
                    error = "Failed to close listener";
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
        }
    }
}
