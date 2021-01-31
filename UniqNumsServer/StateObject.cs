using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UniqNumsServer
{
    public class StateObject
    {
        // Size of receive buffer - allows 10000 numbers to be written at once
        public const int BufferSize = 110000;
        
        // Receive buffer
        public byte[] buffer = new byte[BufferSize];

        // Received data string
        public StringBuilder sb = new StringBuilder();

        // Client socket'
        public Socket workSocket = null;
    }
}
