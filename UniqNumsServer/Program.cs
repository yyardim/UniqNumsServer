using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UniqNumsServer
{
    public class NumberListener
    {
        public static int Main(string[] args)
        {
            StartServer();
            return 0;
        }

        public static void StartServer()
        {
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 4000);

            try
            {
                // Create a Socket that will use TCP protocol
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response
                listener.Listen(5);

                Console.WriteLine("Waiting for a connection...");
                Socket handler = listener.Accept();

                // Incoming data from the client
                string data = null; ///TODO Use stringbuilder instead
                byte[] bytes = null;

                while (true)
                {
                    // Environment.NewLine, which is \r\n is 2 bytes. Decimal is 16  bytes. So we expect 18 bytes.
                    bytes = new byte[18];
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf(Environment.NewLine) > -1)
                    {
                        break;
                    }
                }

                Console.WriteLine("Text received: {0}", data);

                byte[] msg = Encoding.ASCII.GetBytes(data);
                handler.Send(msg); ///We aren't sending anything to client
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\n Press any key to continue");
            Console.ReadLine();
        }
    }
}
