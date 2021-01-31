using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NumsClient
{
    // Client app is sending messages to a Server/Listener
    // Both listener & client can send messages back & forth once a communication is established
    public class NumsClient
    {
        // The port number for the remote device
        private const int port = 4000;

        // ManualResetEvent instances signal completion
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);

        public static int Main(string[] args)
        {
            StartClient();
            return 0;
        }

        public static void StartClient()
        {
            // Connect to a remote device
            try {
                // Establish the remote endpoint for the socket
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket
                Socket client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endoint
                client.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send data to the remote device
                Send(client, "123456789" + Environment.NewLine);
                sendDone.WaitOne();

                // Release the socket
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            }

            #region
            //byte[] bytes = new byte[18];

            //try
            //{
            //    // Connect to a Remote server
            //    // Get Host IP Address that is used to establish a connection
            //    IPHostEntry host = Dns.GetHostEntry("localhost");
            //    IPAddress ipAddress = host.AddressList[0];
            //    IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, 4000);

            //    // Create a TCP/IP socket
            //    Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //    // Connect the socket to the remote endpoint
            //    try
            //    {
            //        // Connect to Remote Endpoint
            //        sender.Connect(remoteEndPoint);

            //        Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());

            //        // Encode the data string into a byte array
            //        byte[] msg = Encoding.ASCII.GetBytes("123456789" + Environment.NewLine); //replace <EOF> with proper eof

            //        // Send the data through the socket
            //        int bytesSent = sender.Send(msg);

            //        // Receive the response from the remote device
            //        int bytesRec = sender.Receive(bytes);
            //        Console.WriteLine("Echoed test = {0}", Encoding.ASCII.GetString(bytes, 0, bytesRec));

            //        // Release the socket
            //        sender.Shutdown(SocketShutdown.Both);
            //        sender.Close();
            //    }
            //    catch (ArgumentNullException argNullExc)
            //    {
            //        Console.WriteLine("ArgumentNullException: {0}", argNullExc.ToString());
            //    }
            //    catch(SocketException sockExc)
            //    {
            //        Console.WriteLine("SocketException: {0}", sockExc.ToString());
            //    }
            //    catch(Exception exc)
            //    {
            //        Console.WriteLine("Unexpected exception: {0}", exc.ToString());
            //    }
            //}
            //catch (Exception exc)
            //{
            //    Console.WriteLine(exc.ToString());
            //}
            #endregion
        }

        private static void ConnectCallback(IAsyncResult asyncResult) {
            try {
                // Retrieve the socket from the state object
                Socket client = (Socket)asyncResult.AsyncState;

                // Complete the connection
                client.EndConnect(asyncResult);

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                // Signal that the connection has been made
                connectDone.Set();
            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            }
        }

        private static void Send(Socket client, String data) {
            // Convert the string data to byte data using ASCII encoding
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device
            client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult asyncResult) {
            try {
                // Retrieve the socket from the state object
                Socket client = (Socket)asyncResult.AsyncState;

                // complete sending the data to the remote device
                int bytesSent = client.EndSend(asyncResult);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent
                sendDone.Set();
            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            }
        }
    }
}
