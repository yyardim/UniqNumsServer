﻿using System.Net.Sockets;
using System.Text;

namespace NumsClient {
    public class StateObject {
        public Socket workSocket = null;
        public const int BufferSize = 50000000;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
    }
}
