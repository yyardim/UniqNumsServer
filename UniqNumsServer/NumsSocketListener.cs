using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public static ConcurrentBag<int> concurrentNumsBag = new ConcurrentBag<int>();
        public static IEnumerable<int> distinctConcurrentNumsBag = new List<int>();
        public static IEnumerable<int> allNumsBag = new List<int>();
        public static IEnumerable<int> newUniqNums = new List<int>();
        public static int uniqNumCount = 0;
        public static int dupNumCount = 0;
        public static int uniqNumTotal = 0;
        public static int counter = 0;
        
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
            handler.ReceiveBufferSize = 50000000;
            handler.SendBufferSize = 50000000;
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

                    string[] numsArray = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    try {
                        int[] intNumsArray = Array.ConvertAll<string, int>(numsArray, int.Parse);
                        Parallel.For(0, intNumsArray.Length, i => {
                            if (Math.Floor(Math.Log10(intNumsArray[i]) + 1) <= 9) {
                                concurrentNumsBag.Add(intNumsArray[i]);
                                Interlocked.Increment(ref counter);
                            } else {
                                handler.Disconnect(true);
                                return;
                            }
                        });
                        var lockObject = new object();
                        lock (lockObject) {
                            // first get the distinct numbers from the generated list above
                            distinctConcurrentNumsBag = concurrentNumsBag.Distinct().ToList();

                            // get new unique numbers from the distinct list comparing with all numbers from previous runs
                            newUniqNums = distinctConcurrentNumsBag.Except(allNumsBag).ToList();

                            // add new unique numbers to the all numbers
                            allNumsBag = allNumsBag.Union(newUniqNums).ToList();

                            dupNumCount = concurrentNumsBag.Count - newUniqNums.Count();
                            uniqNumCount = newUniqNums.Count();
                            uniqNumTotal += uniqNumCount;
                            concurrentNumsBag = new ConcurrentBag<int>();
                        }

                        GenerateNumbersLog(newUniqNums, -1);
                    } catch (Exception exc) {
                        Console.WriteLine(exc.ToString());
                    } finally {
                        handler.Disconnect(true);
                    }
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
        private static void GenerateNumbersLog(IEnumerable<int> numbers, int timeout) {
            var numbersLog = Path.Combine(Directory.GetCurrentDirectory(), "numbers.log");

            try {
                writer.TryEnterWriteLock(timeout);

                if (!File.Exists(numbersLog)) {
                    File.WriteAllLines(numbersLog, numbers.Select(n => n.ToString()));
                } else {
                    File.AppendAllLines(numbersLog, numbers.Select(n => n.ToString()));
                }
            } catch (Exception exc) {
                Console.WriteLine(exc.ToString());
            } finally {
                writer.ExitWriteLock();
            }
        }
    }
}
