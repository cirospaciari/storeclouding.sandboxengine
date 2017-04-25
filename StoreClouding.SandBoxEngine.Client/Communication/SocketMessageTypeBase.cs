using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Communication
{
    public abstract class SocketMessageTypeBase
    {
        public SocketControllerApplication Controller { get; set; }

        public abstract int MessageTypeID { get; }

        public abstract void OnMessageReceived(byte[] message);

        public abstract bool Update(out string error);

        public void Send(byte[] message)
        {
            Controller.Send(MessageTypeID, message);
        }

    }
}
