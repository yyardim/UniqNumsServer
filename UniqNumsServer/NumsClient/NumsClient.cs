﻿using System;
using System.Collections.Generic;
using System.IO;
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

                string nums = ReadNums();

                // Send data to the remote device
                Send(client, nums);
                sendDone.WaitOne();

                // Release the socket
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            }
        }

        /// <summary>
        /// Reads numbers from "nums.txt" file that should be placed in the same directory as the client application
        /// The assumption of the file is similar to the server requirements, where each number is in its own line
        /// Skipping any error validations here as this client app is not the target but it's merely here to send numbers to the listener
        /// </summary>
        /// <returns>string</returns>
        private static string ReadNums() {
            var numsFile = Path.Combine(Directory.GetCurrentDirectory(), "nums.txt");
            var sbNums = new StringBuilder();
            if (File.Exists(numsFile)) {
                string[] lines = File.ReadAllLines(numsFile);
                foreach (var line in lines) {
                    sbNums.Append(line + Environment.NewLine);
                }
            }

            return sbNums.ToString();
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
