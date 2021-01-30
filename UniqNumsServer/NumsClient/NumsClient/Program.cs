using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NumsClient
{
    // Client app is sending messages to a Server/Listener
    // Both listener & client can send messages back & forth once a communication is established
    public class NumsClient
    {
        public static int Main(string[] args)
        {
            StartClient();
            return 0;
        }

        public static void StartClient()
        {
            byte[] bytes = new byte[18];

            try
            {
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, 4000);

                // Create a TCP/IP socket
                Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint
                try
                {
                    // Connect to Remote Endpoint
                    sender.Connect(remoteEndPoint);

                    Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array
                    byte[] msg = Encoding.ASCII.GetBytes("123456789" + Environment.NewLine); //replace <EOF> with proper eof

                    // Send the data through the socket
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}", Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    // Release the socket
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException argNullExc)
                {
                    Console.WriteLine("ArgumentNullException: {0}", argNullExc.ToString());
                }
                catch(SocketException sockExc)
                {
                    Console.WriteLine("SocketException: {0}", sockExc.ToString());
                }
                catch(Exception exc)
                {
                    Console.WriteLine("Unexpected exception: {0}", exc.ToString());
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }
    }
}
