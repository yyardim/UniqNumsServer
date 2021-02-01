using System.Net.Sockets;
using System.Text;

namespace UniqNumsServer {
    public class StateObject
    {
        // Size of receive buffer 50,000,000 bytes - allows over 4.5M numbers to be written at once
        public const int BufferSize = 50000000;
        
        // Receive buffer
        public byte[] buffer = new byte[BufferSize];

        // Received data string
        public StringBuilder sb = new StringBuilder();

        // Client socket'
        public Socket workSocket = null;
    }
}
