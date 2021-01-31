using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UniqNumsServer {
    /// <summary>
    /// Asynchronous server socket proceses network service requests
    /// </summary>
    public class NumsSocketListener
    {
        // boolean variable in memory. When false, all threads are blocked, and when true, all threads are unblocked
        // below it is set to false, so all threads that call WaitOne() will block until some thread calls the Set() method.
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public static ReaderWriterLockSlim writer = new ReaderWriterLockSlim();
        public static ConcurrentBag<string> concurrentNumsBag = new ConcurrentBag<string>();
        public static int uniqNumCount = 0;
        public static int dupNumCount = 0;
        public static int uniqNumTotal = 0;
        
        /// <summary>
        /// Empty Ctor
        /// </summary>
        public NumsSocketListener()
        {
        }
        /// <summary>
        /// Main Method
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
        {
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

            Console.WriteLine($"UniqNumsServer has started! Waiting for connections at the port: {localEndPoint}");
            Console.WriteLine("Received\n\r");
            // Start timer for console report
            Timer timer = new Timer(ConsoleReport, null, 0, 10000);

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

        /// <summary>
        /// Private method to write Console Report
        /// </summary>
        /// <param name="stateInfo"></param>
        private static void ConsoleReport(Object stateInfo) {
            Console.WriteLine("{0} unique numbers, {1} duplicates. Unique total: {2}", uniqNumCount, dupNumCount, uniqNumTotal);
            uniqNumCount = 0;
            dupNumCount = 0;
        }

        /// <summary>
        /// Public callback that accepts IAsyncResult and sets up listener. The BeginReceive is within this method
        /// Accepts buffer & ReadCallback
        /// </summary>
        /// <param name="asyncResult"></param>
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

        /// <summary>
        /// ReadCallback - read bytes and converts to string array with split of newlines
        /// Line lengths are verified to be 9 and to be digits only
        /// If "terminate" line is found, program shuts down
        /// If non-unique lines are found, client is disconnected.
        /// Thread safe counters are set
        /// Finally GenerateNumbersLog method is called to write the unique numbers in the log file
        /// </summary>
        /// <param name="asyncResult"></param>
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

                // check "terminate" case independent & shutdown if exists
                if (content.IndexOf("terminate", StringComparison.CurrentCultureIgnoreCase) > -1) {
                    try {
                        handler.Shutdown(SocketShutdown.Both);
                    } catch (Exception exc) {
                        Console.WriteLine(exc.ToString());
                        throw;
                    } finally {
                        handler.Close();
                        System.Environment.Exit(0);
                    }
                }

                // check lines have a newline; if so, form a concurrent array by spliting the nums string and work on numbers in a loop
                if (content.IndexOf(Environment.NewLine) > -1) {
                    var numbersToWrite = new List<string>();

                    string[] numsArray = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var num in numsArray) {
                        // Proceed if the number length is 9 and it is digits only. Otherwise disconnect the client & return
                        if ((num.Length == 9 && IsDigitsOnly(num)) || num != string.Empty) {
                            // If unique, increment unique counter & add to the concurrentBag - Else, increment dup counter
                            if (concurrentNumsBag.Any(n => n == num)) {
                                Interlocked.Increment(ref dupNumCount);
                            } else {
                                concurrentNumsBag.Add(num);
                                numbersToWrite.Add(num);
                                Interlocked.Increment(ref uniqNumCount);
                                Interlocked.Increment(ref uniqNumTotal);
                            }
                        } else {
                            handler.Disconnect(true);
                            return;
                        }
                    }

                    GenerateNumbersLog(numbersToWrite, -1);

                } else {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        /// <summary>
        /// Private Method to create/append numbers.log file with provided numbers list
        /// </summary>
        /// <param name="numbers"></param>
        /// <param name="timeout"></param>
        private static void GenerateNumbersLog(List<string> numbers, int timeout) {
            var numbersLog = Path.Combine(Directory.GetCurrentDirectory(), "numbers.log");

            try {
                writer.TryEnterWriteLock(timeout);

                if (!File.Exists(numbersLog)) {
                    File.WriteAllLines(numbersLog, numbers);
                } else {
                    File.AppendAllLines(numbersLog, numbers);
                }
            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            } finally {
                writer.ExitWriteLock();
            }
        }
        /// <summary>
        /// Check the number and validate it is formed of digits only
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static bool IsDigitsOnly(string num) {
            foreach (char c in num) {
                if (c < '0' || c > '9') {
                    return false;
                }
            }
            return true;
        }
    }
}
