using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniqNumsServer
{
    /// <summary>
    /// Asynchronous server socket proceses network service requests
    /// </summary>
    public class NumsSocketListener
    {
        // boolean variable in memory. When false, all threads are blocked, and when true, all threads are unblocked
        // below it is set to false, so all threads that call WaitOne() will block until some thread calls the Set() method.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public NumsSocketListener()
        {
        }

        public static int Main(string[] args)
        {
            //StartServer();
            StartListening();
            return 0;
        }

        /// <summary>
        /// Socket is initialized for the PORT#: 40000 as listener
        /// listener starts listening & accepting up to 5 client connections via BeginAccept
        /// </summary>
        public static void StartListening()
        {
            // Establish the local endpoint for the socket.
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPEndPoint localEndPoint = new IPEndPoint(host.AddressList[0], 4000);

            Console.WriteLine($"Local address & port: {localEndPoint}");

            // Create a TCP/IP socket
            Socket listener = new Socket(localEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(5);
                
                while (true)
                {
                    // Set the event to nonsignaled state, bool becomes false & thread is locked
                    allDone.Reset();

                    // Start an async socket to listen for connections
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing & block the thread in the meantime
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("Closing the listener...");
        }

        public static void AcceptCallback(IAsyncResult asyncResult)
        {
            // Signals all the waiting threads to continue
            allDone.Set();

            // Get the socket that handles the client requst
            Socket listener = (Socket) asyncResult.AsyncState;
            Socket handler = listener.EndAccept(asyncResult);

            // Create the state object
            var state = new StateObject
            {
                workSocket = handler
            };
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult asyncResult) {
            String content = String.Empty;

            // Retrieve the state object and the handler socket from the asynchronous state object
            StateObject state = (StateObject)asyncResult.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket
            int bytesRead = handler.EndReceive(asyncResult);

            if (bytesRead > 0) {
                // There might be more data, so store the data received so far
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                // check for end-of-file tag. If it's not there, read more data
                content = state.sb.ToString();
                if (content.IndexOf(Environment.NewLine) > -1) {
                    // All the data has been read from the client. Display it on the console
                    Console.WriteLine("Read {0} bytes from socket. \n Data: {1}", content.Length, content);

                    // Echo the data back to the client. THIS NOT NEEDED
                    //Send(handler, content);
                } else {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        public static void StartServer()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
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
                    bytes = new byte[1800];
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
