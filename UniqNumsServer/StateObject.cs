﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UniqNumsServer
{
    public class StateObject
    {
        // Size of receive buffer
        public const int BufferSize = 1024;
        
        // Receive buffer
        public byte[] buffer = new byte[BufferSize];

        // Received data string
        public StringBuilder sb = new StringBuilder();

        // Client socket'
        public Socket workSocket = null;
    }
}
