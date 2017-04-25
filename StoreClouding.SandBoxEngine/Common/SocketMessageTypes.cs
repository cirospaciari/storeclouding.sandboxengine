using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Common
{
    enum SocketMessageTypes : int
    {
        Ping = 0,
        Disconnected = 1,
        Terrain = 2
    }
}
