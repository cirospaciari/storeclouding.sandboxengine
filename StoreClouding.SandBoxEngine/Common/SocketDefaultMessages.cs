﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Common
{
    class SocketDefaultMessages
    {
        public static byte[] Disconnected = new byte[] { 255 };
        public static byte[] Ping = new byte[] { 255 };

        public static byte[] TerrainStartPointEnded = new byte[] { 255 };
    }
}
