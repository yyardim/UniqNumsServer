# UniqNumsServer
Project Structure
- UniqNumsServer
  - NumsClient
  - TestClients

Description of Project Structure
There are 2 solutions within this repository:

- UniqNumsServer - The async/multi-thread socket listener allows up to 5 clients to connect asynchronously and waits for the transmission of a stream of bytes, which will be converted into 9 digit numbers. Finally, these numbersare saved in a log file called "numbers.log". The "numbers.log" file does not include any duplicates and each number is written in a single line. The socket listener also provides a summary of numbers including unique count in the batch, duplicate count in the batch and the total unique count that is written into the log file. This report is generated every 10 seconds and written in the console window that stays open during the life time of the application. The buffer size is set to 110,000 bytes in the socket listener, which allows 10,000 9-digit numbers. The read/write operation of 10K numbers are instanteneous. If one of the lines has the text "terminate", the socket listener shuts down and closes all connections. If any of the clients sends an invalid form of data (non-numeric) or not 9 digit, the socket listener disconnects the client without providing any feedback.

- NumsClient - This is a simple async socket client that connects to the server from the port 4000. Within the same location of the executable, an assumption is made that there's a file called "nums.txt" that includes 9 digit numbers and a line break at the end of each number. The maximum expected number of lines is 10,000. Once the client is executed, a console window appears briefly while the execution is in progress and content of the "nums.txt" is transmitted to the socket listener. The socket listener read/validates/writes the content eliminating the duplicates in another file and also provide an on-screen report of the total numbers including batch duplicates for the client, batch uniques and total uniques.

Instructions for local debugging:
- Open \UniqueNumsServer\UniqNumsServer\UniqNumsServer.sln in Visual Studio - This is the server
- Open \UniqueNumsServer\UniqNumsServer\NumsClient\NumsClient.sln in Visual Studio - This is the client
- Open \UniqueNumsServer\UniqNumsServer\TestClients folder, to access sample test nums.txt files. There are 5 client folders, choose Client5 for debugging purpose. To test the server, you can execute NumsClient.exe from the \UniqueNumsServer\UniqNumsServer\TestClients\Client5 folder directly. Or to test both the server & client simultenaously, copy the nums.txt file into \UniqueNumsServer\UniqNumsServer\NumsClient\bin\Debug\ folder first. Then start debugging the NumsClient project while UniqNumsServer project is already running. Now the file will be transmitted to the server.

- Future Release will include the following:
  - The buffer size is takern into a configuration so that it could be adjusted by the user
  - The name & location of the input number document is taken into a configuration so that it could be adjusted by the user
