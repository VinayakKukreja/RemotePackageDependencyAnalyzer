/////////////////////////////////////////////////////////////////////
// MPCommService.cs - service for MessagePassingComm               //
// ver 2.1                                                         //
// Jim Fawcett, CSE681-OnLine, Summer 2017                 //
/////////////////////////////////////////////////////////////////////
/*
 * Started this project with C# Console Project wizard
 * - Added references to:
 *   - System.ServiceModel
 *   - System.Runtime.Serialization
 *   - System.Threading;
 *   - System.IO;
 *   
 * Package Operations:
 * -------------------
 * This package defines three classes:
 * - Sender which implements the public methods:
 *   -------------------------------------------
 *   - Sender           : constructs sender using address string and port
 *   - connect          : opens channel and attempts to connect to an endpoint, 
 *                        trying multiple times to send a connect message
 *   - close            : closes channel
 *   - postMessage      : posts to an internal thread-safe blocking queue, which
 *                        a sendThread then dequeues msg, inspects for destination,
 *                        and calls connect(address, port)
 *   - postFile         : attempts to upload a file in blocks
 *   - close            : closes current connection
 *   - getLastError     : returns exception messages on method failure
 *
 * - Receiver which implements the public methods:
 *   ---------------------------------------------
 *   - Receiver         : constructs Receiver instance
 *   - start            : creates instance of ServiceHost which services incoming messages
 *                        using address string and port of listener
 *   - postMessage      : Sender proxies call this message to enqueue for processing
 *   - getMessage       : called by Receiver application to retrieve incoming messages
 *   - close            : closes ServiceHost
 *   - openFileForWrite : opens a file for storing incoming file blocks
 *   - writeFileBlock   : writes an incoming file block to storage
 *   - closeFile        : closes newly uploaded file
 *   - size             : returns number of messages waiting in receive queue
 *   
 * - Comm which implements, using Sender and Receiver instances, the public methods:
 *   -------------------------------------------------------------------------------
 *   - Comm             : create Comm instance with address and port
 *   - postMessage      : send CommMessage instance to a Receiver instance
 *   - getMessage       : retrieves a CommMessage from a Sender instance
 *   - postFile         : called by a Sender instance to transfer a file
 *   - close()          : stops sender and receiver threads
 *   - restart          : attempts to restart with port - that must be different from
 *                        any port previously used while the embedding process states alive
 *   - closeConnection  : closes current connection, can reopen that or another connection
 *   - size             : returns number of messages in receive queue
 *    
 * The Package also implements the class TestPCommService with public methods:
 * ---------------------------------------------------------------------------
 * - testSndrRcvr()     : test instances of Sender and Receiver
 * - testComm()         : test Comm instance
 * - compareMsgs        : compare two CommMessage instances for near equality
 * - compareFileBytes   : compare two files byte by byte
 *
 *
 * Required Files:
 * ---------------
 * IMPCommService.cs, MPCommService.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 3.0 : 26 Oct 2017
 * - Receiver receive thread processing changed to discard connect messages
 * - added close, size, and restart functions
 * ver 2.1 : 20 Oct 2017
 * - minor changes to these comments
 * ver 2.0 : 19 Oct 2017
 * - renamed namespace and several components
 * - eliminated IPluggable.cs
 * ver 1.0 : 14 Jun 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Threading;
using Navigator;
using SWTools;

namespace MessagePassingComm
{
    ///////////////////////////////////////////////////////////////////
    // Receiver class - receives CommMessages and Files from Senders

    public class Receiver : IMessagePassingComm
    {
        private ServiceHost commHost;
        private FileStream fs;
        private string lastError = "";

        /*----< constructor >------------------------------------------*/

        public Receiver()
        {
            if (rcvQ == null)
                rcvQ = new BlockingQueue<CommMessage>();
        }

        public static BlockingQueue<CommMessage> rcvQ { get; set; }

        public bool restartFailed { get; set; }
        /*----< enqueue a message for transmission to a Receiver >-----*/

        public void postMessage(CommMessage msg)
        {
            msg.threadId = Thread.CurrentThread.ManagedThreadId;
            TestUtilities.putLine(string.Format("sender enqueuing message on thread {0}",
                Thread.CurrentThread.ManagedThreadId));
            rcvQ.enQ(msg);
        }
        /*----< retrieve a message sent by a Sender instance >---------*/

        public CommMessage getMessage()
        {
            var msg = rcvQ.deQ();
            if (msg.type == CommMessage.MessageType.closeReceiver) close();
            if (msg.type == CommMessage.MessageType.connect) msg = rcvQ.deQ(); // discarding the connect message
            return msg;
        }
        /*---< called by Sender's proxy to open file on Receiver >-----*/

        public bool openFileForWrite(string name)
        {
            try
            {
                var writePath = Path.Combine(ClientEnvironment.root, name);
                fs = File.OpenWrite(writePath);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< write a block received from Sender instance >----------*/

        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< close Receiver's uploaded file >-----------------------*/

        public void closeFile()
        {
            fs.Close();
        }

        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
       * baseAddress is of the form: http://IPaddress or http://networkName
       */
        public bool start(string baseAddress, int port)
        {
            try
            {
                var address = baseAddress + ":" + port + "/IMessagePassingComm";
                TestUtilities.putLine(string.Format("starting Receiver on thread {0}",
                    Thread.CurrentThread.ManagedThreadId));
                createCommHost(address);
                restartFailed = false;
                return true;
            }
            catch (Exception ex)
            {
                restartFailed = true;
                Console.Write("\n{0}\n", ex.Message);
                Console.Write("\n  You can't restart a listener on a previously used port");
                Console.Write(" - Windows won't release it until the process shuts down");
                return false;
            }
        }

        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
       * address is of the form: http://IPaddress:8080/IMessagePassingComm
       */
        public void createCommHost(string address)
        {
            var binding = new WSHttpBinding();
            var baseAddress = new Uri(address);
            commHost = new ServiceHost(typeof(Receiver), baseAddress);
            commHost.AddServiceEndpoint(typeof(IMessagePassingComm), binding, baseAddress);
            commHost.Open();
        }
        /*----< how many messages in receive queue? >-----------------*/

        public int size()
        {
            return rcvQ.size();
        }
        /*----< close ServiceHost >----------------------------------*/

        public void close()
        {
            Console.Write("\n  closing receiver - please wait \n");
            commHost.Close();
            (commHost as IDisposable).Dispose();

            Console.Write("\n  commHost.Close() returned \n");
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Sender class - sends messages and files to Receiver

    public class Sender
    {
        private readonly int maxCount = 10;
        private readonly BlockingQueue<CommMessage> sndQ;
        private readonly Thread sndThread;
        private IMessagePassingComm channel;
        private ChannelFactory<IMessagePassingComm> factory;
        private string fromAddress = "";
        private string lastError = "";
        private string lastUrl = "";
        private int port;
        private string toAddress = "";
        private int tryCount;

        /*----< constructor >------------------------------------------*/

        public Sender(string baseAddress, int listenPort)
        {
            port = listenPort;
            fromAddress = baseAddress + listenPort + "/IMessagePassingComm";
            sndQ = new BlockingQueue<CommMessage>();
            TestUtilities.putLine(string.Format("starting Sender on thread {0}", Thread.CurrentThread.ManagedThreadId));
            sndThread = new Thread(threadProc);
            sndThread.Start();
        }
        /*----< creates proxy with interface of remote instance >------*/

        public void createSendChannel(string address)
        {
            var baseAddress = new EndpointAddress(address);
            var binding = new WSHttpBinding();
            factory = new ChannelFactory<IMessagePassingComm>(binding, address);
            channel = factory.CreateChannel();
        }
        /*----< attempts to connect to Receiver instance >-------------*/

        public bool connect(string baseAddress, int port)
        {
            toAddress = baseAddress + ":" + port + "/IMessagePassingComm";
            return connect(toAddress);
        }

        /*----< attempts to connect to Receiver instance >-------------*/
        /*
       * - attempts a finite number of times to connect to a Receiver
       * - first attempt to send will throw exception of no listener
       *   at the specified endpoint
       * - to test that we attempt to send a connect message
       */
        public bool connect(string toAddress)
        {
            var timeToSleep = 500;
            TestUtilities.putLine("attempting to connect to \"" + toAddress + "\"");
            createSendChannel(toAddress);
            var connectMsg = new CommMessage(CommMessage.MessageType.connect);
            while (true)
                try
                {
                    channel.postMessage(connectMsg);
                    tryCount = 0;
                    return true;
                }
                catch (Exception ex)
                {
                    if (++tryCount < maxCount)
                    {
                        TestUtilities.putLine("failed to connect - waiting to try again");
                        Thread.Sleep(timeToSleep);
                    }
                    else
                    {
                        TestUtilities.putLine("failed to connect - quitting");
                        lastError = ex.Message;
                        return false;
                    }
                }
        }
        /*----< closes Sender's proxy >--------------------------------*/

        public void close()
        {
            while (sndQ.size() > 0)
            {
                var msg = sndQ.deQ();
                try
                {
                    channel.postMessage(msg);
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }

            try
            {
                if (factory != null)
                    factory.Close();
            }
            catch (Exception ex)
            {
                Console.Write("\n  already closed\n");
            }
        }

        /*----< processing for send thread >--------------------------*/
        /*
       * - send thread dequeues send message and posts to channel proxy
       * - thread inspects message and routes to appropriate specified endpoint
       */
        private void threadProc()
        {
            while (true)
            {
                TestUtilities.putLine(string.Format("sender enqueuing message on thread {0}",
                    Thread.CurrentThread.ManagedThreadId));

                var msg = sndQ.deQ();
                if (msg.type == CommMessage.MessageType.closeSender)
                {
                    TestUtilities.putLine("Sender send thread quitting");
                    break;
                }

                if (msg.to == lastUrl)
                {
                    channel.postMessage(msg);
                }
                else
                {
                    close();
                    if (!connect(msg.to))
                        continue;
                    lastUrl = msg.to;
                    channel.postMessage(msg);
                }
            }
        }
        /*----< main thread enqueues message for sending >-------------*/

        public void postMessage(CommMessage msg)
        {
            sndQ.enQ(msg);
        }
        /*----< uploads file to Receiver instance >--------------------*/

        public bool postFile(string fileName)
        {
            FileStream fs = null;
            long bytesRemaining;
            var directory = Directory.GetParent(Directory.GetParent(ServerEnvironment.root).ToString()).ToString();
            try
            {
                var path = Path.Combine(directory, fileName);
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                channel.openFileForWrite(fileName);
                while (true)
                {
                    var bytesToRead = Math.Min(ServerEnvironment.blockSize, bytesRemaining);
                    var blk = new byte[bytesToRead];
                    long numBytesRead = fs.Read(blk, 0, (int) bytesToRead);
                    bytesRemaining -= numBytesRead;

                    channel.writeFileBlock(blk);
                    if (bytesRemaining <= 0)
                        break;
                }

                channel.closeFile();
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }

            return true;
        }


        public bool postFile(string pathIncoming, string fileName)
        {
            FileStream fs = null;
            long bytesRemaining;
            //string directory = Directory.GetParent(Directory.GetParent(ServerEnvironment.root).ToString()).ToString();
            var directory = pathIncoming;
            try
            {
                var path = Path.Combine(directory, fileName);
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                channel.openFileForWrite(fileName);
                while (true)
                {
                    var bytesToRead = Math.Min(ServerEnvironment.blockSize, bytesRemaining);
                    var blk = new byte[bytesToRead];
                    long numBytesRead = fs.Read(blk, 0, (int) bytesToRead);
                    bytesRemaining -= numBytesRead;

                    channel.writeFileBlock(blk);
                    if (bytesRemaining <= 0)
                        break;
                }

                channel.closeFile();
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }

            return true;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Comm class combines Receiver and Sender

    public class Comm
    {
        private readonly string address;
        private readonly int portNum;
        private Receiver rcvr;
        private Sender sndr;

        /*----< constructor >------------------------------------------*/
        /*
       * - starts listener listening on specified endpoint
       */
        public Comm(string baseAddress, int port)
        {
            address = baseAddress;
            portNum = port;
            rcvr = new Receiver();
            rcvr.start(baseAddress, port);
            sndr = new Sender(baseAddress, port);
        }
        /*----< shutdown comm >----------------------------------------*/

        public void close()
        {
            Console.Write("\n  Comm closing");
            rcvr.close();
            sndr.close();
        }
        /*----< restart comm >-----------------------------------------*/

        public bool restart(int newport)
        {
            rcvr = new Receiver();
            rcvr.start(address, newport);
            if (rcvr.restartFailed) return false;
            sndr = new Sender(address, portNum);
            return true;
        }
        /*----< closes connection but keeps comm alive >---------------*/

        public void closeConnection()
        {
            sndr.close();
        }
        /*----< post message to remote Comm >--------------------------*/

        public void postMessage(CommMessage msg)
        {
            sndr.postMessage(msg);
        }
        /*----< retrieve message from remote Comm >--------------------*/

        public CommMessage getMessage()
        {
            return rcvr.getMessage();
        }
        /*----< called by remote Comm to upload file >-----------------*/

        public bool postFile(string filename)
        {
            return sndr.postFile(filename);
        }

        public bool postFile(string path, string filename)
        {
            return sndr.postFile(path, filename);
        }
        /*----< how many messages in receive queue? >-----------------*/

        public int size()
        {
            return rcvr.size();
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestPCommService class - tests Receiver, Sender, and Comm

    internal class TestPCommService
    {
        /*----< collect file names from client's FileStore >-----------*/

        public static List<string> getClientFileList()
        {
            var names = new List<string>();
            var files = Directory.GetFiles(ClientEnvironment.root);
            foreach (var file in files) names.Add(Path.GetFileName(file));
            return names;
        }

        /*----< compare CommMessages property by property >------------*/
        /*
       * - skips threadId property
       */
        public static bool compareMsgs(CommMessage msg1, CommMessage msg2)
        {
            var t1 = msg1.type == msg2.type;
            var t2 = msg1.to == msg2.to;
            var t3 = msg1.from == msg2.from;
            var t4 = msg1.author == msg2.author;
            var t5 = msg1.command == msg2.command;
            //bool t6 = (msg1.threadId == msg2.threadId);
            var t7 = msg1.errorMsg == msg2.errorMsg;
            if (msg1.arguments.Count != msg2.arguments.Count)
                return false;
            for (var i = 0; i < msg1.arguments.Count; ++i)
                if (msg1.arguments[i] != msg2.arguments[i])
                    return false;
            return t1 && t2 && t3 && t4 && t5 && /*t6 &&*/ t7;
        }
        /*----< compare binary file's bytes >--------------------------*/

        private static bool compareFileBytes(string filename)
        {
            TestUtilities.putLine(string.Format("testing byte equality for \"{0}\"", filename));

            var fileSpec1 = Path.Combine(ClientEnvironment.root, filename);
            var fileSpec2 = Path.Combine(ServerEnvironment.root, filename);
            try
            {
                var bytes1 = File.ReadAllBytes(fileSpec1);
                var bytes2 = File.ReadAllBytes(fileSpec2);
                if (bytes1.Length != bytes2.Length)
                    return false;
                for (var i = 0; i < bytes1.Length; ++i)
                    if (bytes1[i] != bytes2[i])
                        return false;
            }
            catch (Exception ex)
            {
                TestUtilities.putLine(string.Format("\n  {0}\n", ex.Message));
                return false;
            }

            return true;
        }
        /*----< test Sender and Receiver classes >---------------------*/

        public static bool testSndrRcvr()
        {
            TestUtilities.vbtitle("testing Sender & Receiver");

            var test = true;
            var rcvr = new Receiver();
            rcvr.start("http://localhost", 8080);
            var sndr = new Sender("http://localhost", 8080);

            var sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "show";
            sndMsg.author = "Jim Fawcett";
            sndMsg.to = "http://localhost:8080/IMessagePassingComm";
            sndMsg.from = "http://localhost:8080/IMessagePassingComm";

            sndr.postMessage(sndMsg);
            CommMessage rcvMsg;
            rcvMsg = rcvr.getMessage();
            if (ClientEnvironment.verbose)
                rcvMsg.show();
            if (!compareMsgs(sndMsg, rcvMsg))
                test = false;
            TestUtilities.checkResult(test, "sndMsg equals rcvMsg");
            TestUtilities.putLine();

            sndMsg.type = CommMessage.MessageType.closeReceiver;
            sndr.postMessage(sndMsg);
            rcvMsg = rcvr.getMessage();
            if (ClientEnvironment.verbose)
                rcvMsg.show();
            if (!compareMsgs(sndMsg, rcvMsg))
                test = false;
            TestUtilities.checkResult(test, "Close Receiver");
            TestUtilities.putLine();

            sndMsg.type = CommMessage.MessageType.closeSender;
            if (ClientEnvironment.verbose)
                sndMsg.show();
            sndr.postMessage(sndMsg);
            // rcvr.getMessage() would fail because server has shut down
            // no rcvMsg so no compare

            TestUtilities.putLine("last message received\n");
            return test;
        }

        /*----< test Comm instance >-----------------------------------*/
        /*
       * - Note: change every occurance of string "Odin" to your machine name
       * 
       */
        public static bool testComm()
        {
            TestUtilities.vbtitle("testing Comm");
            var test = true;

            var comm = new Comm("http://localhost", 8081);
            var csndMsg = new CommMessage(CommMessage.MessageType.request);

            csndMsg.command = "show";
            csndMsg.author = "Jim Fawcett";
            var localEndPoint = "http://localhost:8081/IMessagePassingComm";
            csndMsg.to = localEndPoint;
            csndMsg.from = localEndPoint;

            comm.postMessage(csndMsg);
            var crcvMsg = comm.getMessage();
            if (ClientEnvironment.verbose)
                crcvMsg.show();
            if (!compareMsgs(csndMsg, crcvMsg))
                test = false;
            TestUtilities.checkResult(test, "csndMsg equals crcvMsg");
            TestUtilities.putLine(comm.size() + " messages left in queue");
            TestUtilities.putLine();

            TestUtilities.vbtitle("testing connect to new EndPoint");
            csndMsg.to = "http://Odin:8081/IMessagePassingComm";
            comm.postMessage(csndMsg);
            crcvMsg = comm.getMessage();
            if (ClientEnvironment.verbose)
                crcvMsg.show();
            if (!compareMsgs(csndMsg, crcvMsg))
                test = false;
            TestUtilities.checkResult(test, "csndMsg equals crcvMsg");
            TestUtilities.putLine(comm.size() + " messages left in queue");
            TestUtilities.putLine();

            TestUtilities.vbtitle("testing file transfer");

            var testFileTransfer = true;

            var names = getClientFileList();
            foreach (var name in names)
            {
                TestUtilities.putLine(string.Format("transferring file \"{0}\"", name));
                var transferSuccess = comm.postFile(name);
                TestUtilities.checkResult(transferSuccess, "transfer");
            }

            foreach (var name in names)
                if (!compareFileBytes(name))
                {
                    testFileTransfer = false;
                    break;
                }

            TestUtilities.checkResult(testFileTransfer, "file transfers");
            TestUtilities.putLine(comm.size() + " messages left in queue");
            TestUtilities.putLine();

            TestUtilities.vbtitle("test closeConnection then postMessage");
            comm.closeConnection();
            var newMsg = new CommMessage(CommMessage.MessageType.request);
            newMsg.to = localEndPoint;
            newMsg.from = localEndPoint;
            comm.postMessage(newMsg);
            var reply = comm.getMessage();
            reply.show();
            // if we get here, test passed
            TestUtilities.checkResult(true, "closeSenderConnenction then PostMessage");
            TestUtilities.putLine(comm.size() + " messages left in queue");
            TestUtilities.putLine();

            TestUtilities.vbtitle("test receiver close");
            csndMsg.type = CommMessage.MessageType.closeReceiver;
            if (ClientEnvironment.verbose)
                csndMsg.show();
            comm.postMessage(csndMsg);
            crcvMsg = comm.getMessage();
            if (ClientEnvironment.verbose)
                crcvMsg.show();
            if (!compareMsgs(csndMsg, crcvMsg))
                test = false;
            TestUtilities.checkResult(test, "closeReceiver");
            TestUtilities.putLine(comm.size() + " messages left in queue");
            TestUtilities.putLine();

            csndMsg.type = CommMessage.MessageType.closeSender;
            comm.postMessage(csndMsg);
            if (ClientEnvironment.verbose)
                csndMsg.show();
            TestUtilities.putLine(comm.size() + " messages left in queue");
            // comm.getMessage() would fail because server has shut down
            // no rcvMsg so no compare

            TestUtilities.putLine("last message received\n");

            TestUtilities.putLine("Test comm.restart on same port - expected to fail");

            if (comm.restart(8081))
            {
                var newerMsg = new CommMessage(CommMessage.MessageType.request);
                newerMsg.to = ClientEnvironment.endPoint;
                newerMsg.from = ClientEnvironment.endPoint;
                comm.postMessage(newerMsg);
                var newReply = comm.getMessage();
                newReply.show();
            }
            else
            {
                Console.Write("\n  can't restart but won't fail test");
            }

            return test && testFileTransfer;
        }
        /*----< do the tests >-----------------------------------------*/

        private static void Main(string[] args)
        {
            ClientEnvironment.verbose = true;
            TestUtilities.vbtitle("testing Message-Passing Communication", '=');

            /*----< uncomment to see Sender & Receiver testing >---------*/
            //TestUtilities.checkResult(testSndrRcvr(), "Sender & Receiver");
            //TestUtilities.putLine();

            TestUtilities.checkResult(testComm(), "Comm");
            TestUtilities.putLine();

            TestUtilities.putLine("Press key to quit\n");
            if (ClientEnvironment.verbose)
                Console.ReadKey();
        }
    }
}