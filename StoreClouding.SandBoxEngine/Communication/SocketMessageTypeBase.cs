using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Communication
{
    public abstract class SocketMessageTypeBase
    {
        public SocketControllerApplication Controller {get;set;}

        public abstract int MessageTypeID { get; }

        public abstract void OnMessageReceived(int connectionID, byte[] message);

        public abstract bool Update(out string error);

        public void Kick(int connectionID)
        {
            Controller.Kick(connectionID);
        }

        public void KickAll()
        {
            Controller.KickAll();
        }

        public void Send(int connectionID,byte[] message)
        {
            Controller.Send(connectionID,MessageTypeID, message);
        }

        public void SendForAll(byte[] message)
        {
            Controller.SendToAll(MessageTypeID, message);
        }
    }
}
