////////////////////////////////////////////////////////////////////////////////
// Server.cs - File Server for WPF Remote Code Analyzer Application          //
// ver 2.0                                                                  //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017         //
// Author: Vinayak Kukreja                                                //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines a single Server class that returns file
 * and directory information about its rootDirectory subtree.  It uses
 * a message dispatcher that handles processing of all incoming and outgoing
 * messages. It also helps perform dependency analysis remotely via client invocation.
 *
 *
 * Public Interface:
 * -----------------
 *  Class NavigatorServer:
 *      public NavigatorServer(): initialize server processing
 *
 *
 * Required Files:
 * ----------------
 *   Executive.cs
 *   FileMgr.cs
 *   Parser.cs
 *   IRulesAndActions.cs, RulesAndActions.cs, ScopeStack.cs, Elements.cs
 *   ITokenCollection.cs, Semi.cs, Toker.cs
 *   Display.cs
 *   CsGraph.cs
 *   MPCommService.cs
 *   BlockingQueue.cs
 *   IMPCommService.cs
 *   Environment.cs
 *   Server.cs
 *   
 *
 * Maintanence History:
 * --------------------
 * Source:
 * ver 2.0 - 24 Oct 2017
 * - added message dispatcher which works very well - see below
 * - added these comments
 * ver 1.0 - 22 Oct 2017
 * - first release
 *
 * Author:
 * ver 1.0
 * -First Release
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CodeAnalysis;
using MessagePassingComm;

namespace Navigator
{
    public class NavigatorServer
    {
        private readonly Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
            new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public NavigatorServer()
        {
            initializeEnvironment();
            Console.Title = "Server";
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }

        private IFileMgr localFileMgr { get; }

        private Comm comm { get; set; }

        /*----< set Environment properties needed by server >----------*/
        private void initializeEnvironment()
        {
            Environment.root = ServerEnvironment.root;
            Environment.address = ServerEnvironment.address;
            Environment.port = ServerEnvironment.port;
            Environment.endPoint = ServerEnvironment.endPoint;
        }

        /*----< define how each message will be processed >------------*/
        private void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> getUpFiles = msg =>
            {
                localFileMgr.currentPath = "";
                var temp = string.Empty;
                var send_path = string.Empty;
                var count = 0;


                if (localFileMgr.pathStack.Count == 1 &&
                    localFileMgr.pathStack.Peek().Equals(ServerEnvironment.root))
                {
                    localFileMgr.currentPath = "";

                    if (localFileMgr.pathStack.Count != 0)
                    {
                        localFileMgr.pathStack.Clear();
                        localFileMgr.pathStack.Push(ServerEnvironment.root);
                    }


                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "getUpFiles";
                    reply.arguments = localFileMgr.getFiles().ToList();
                    return reply;
                }
                else
                {
                    temp = localFileMgr.pathStack.Pop();
                    send_path = localFileMgr.pathStack.Peek();

                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "getUpFiles";
                    reply.arguments = localFileMgr.getFiles(send_path).ToList();
                    return reply;
                }
            };
            messageDispatcher["getUpFiles"] = getUpFiles;


            Func<CommMessage, CommMessage> getUpDirs = msg =>
            {
                localFileMgr.currentPath = "";
                var send_path = string.Empty;
                var count = 0;


                if (localFileMgr.pathStack.Count == 1 &&
                    localFileMgr.pathStack.Peek().Equals(ServerEnvironment.root))
                {
                    localFileMgr.currentPath = "";
                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "getUpDirs";
                    reply.arguments = localFileMgr.getDirs().ToList();
                    return reply;
                }
                else
                {
                    send_path = localFileMgr.pathStack.Peek();

                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "getUpDirs";
                    reply.arguments = localFileMgr.getDirs(send_path).ToList();
                    return reply;
                }
            };
            messageDispatcher["getUpDirs"] = getUpDirs;


            Func<CommMessage, CommMessage> connect = msg =>
            {
                localFileMgr.currentPath = "";

                if (localFileMgr.pathStack.Count != 0)
                {
                    localFileMgr.pathStack.Clear();
                    localFileMgr.pathStack.Push(ServerEnvironment.root);
                }


                var reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "connect";
                reply.arguments.Add("Connected");
                return reply;
            };
            messageDispatcher["connect"] = connect;


            Func<CommMessage, CommMessage> getTopFiles = msg =>
            {
                localFileMgr.currentPath = "";

                if (localFileMgr.pathStack.Count != 0)
                {
                    localFileMgr.pathStack.Clear();
                    localFileMgr.pathStack.Push(ServerEnvironment.root);
                }


                var reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopFiles";
                reply.arguments = localFileMgr.getFiles().ToList();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;

            Func<CommMessage, CommMessage> getTopDirs = msg =>
            {
                localFileMgr.currentPath = "";
                var reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopDirs";
                reply.arguments = localFileMgr.getDirs().ToList();
                return reply;
            };
            messageDispatcher["getTopDirs"] = getTopDirs;


            Func<CommMessage, CommMessage> moveIntoFolderFiles = msg =>
            {
                var temp = string.Empty;

                if (msg.arguments.Count() == 1)
                {
                    //temp_path = localFileMgr.pathStack.Peek();
                    temp = localFileMgr.pathStack.Peek() + "/" + msg.arguments[0];
                    localFileMgr.pathStack.Push(temp);
                    localFileMgr.currentPath = msg.arguments[0];
                }

                //string temp = localFileMgr.pathStack.Peek();
                var reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderFiles";
                reply.arguments = localFileMgr.getFiles(temp).ToList();
                return reply;
            };
            messageDispatcher["moveIntoFolderFiles"] = moveIntoFolderFiles;


            Func<CommMessage, CommMessage> moveIntoFolderDirs = msg =>
            {
                var temp = string.Empty;

                if (msg.arguments.Count() == 1) temp = localFileMgr.pathStack.Peek();

                var reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderDirs";
                reply.arguments = localFileMgr.getDirs(temp).ToList();
                return reply;
            };
            messageDispatcher["moveIntoFolderDirs"] = moveIntoFolderDirs;


            Func<CommMessage, CommMessage> Close = msg =>
            {
                var reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "Close";
                reply.arguments.Add("Close");
                return reply;
            };
            messageDispatcher["Close"] = Close;


            Func<CommMessage, CommMessage> DepAnalysis = msg =>
            {
                localFileMgr.currentPath = "";
                var temp = string.Empty;
                var send_path = string.Empty;
                var count = 0;


                try
                {
                    var ex = new Executive();
                    FileStream ostrm;
                    StreamWriter writer;
                    var oldOut = Console.Out;
                    var directory = Directory.GetParent(Directory.GetParent(ServerEnvironment.root).ToString())
                        .ToString();


                    var listOfSelectedFiles = msg.arguments;


                    if (File.Exists(directory + "/Analysis.txt")) File.Delete(directory + "/Analysis.txt");
                    ostrm = new FileStream(directory + "/Analysis.txt", FileMode.Create, FileAccess.Write);
                    writer = new StreamWriter(ostrm);
                    writer.AutoFlush = true;
                    Console.SetOut(writer);


                    send_path = localFileMgr.pathStack.Peek();
                    var temp_list = new List<string> {"/Analysis.txt"};


                    var nav = new Navigate();
                    nav.Add("*.cs");
                    List<string> files;


                    if (listOfSelectedFiles.Count > 0)
                    {
                        nav.go(send_path, listOfSelectedFiles);
                        files = nav.allFiles;
                        ex.getResult(files);
                    }
                    else
                    {
                        nav.go(send_path);
                        files = nav.allFiles;
                        ex.getResult(files);
                    }


                    Console.SetOut(oldOut);
                    writer.Flush();
                    ostrm.Flush();
                    writer.Close();
                    ostrm.Close();


                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "DepAnalysis";
                    reply.arguments = temp_list;


                    return reply;
                }
                catch (Exception e)
                {
                    var temp_list = new List<string> {"/Analysis.txt"};
                    Console.WriteLine(e);
                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "DepAnalysis";
                    reply.arguments = temp_list;
                    return reply;
                }
            };

            messageDispatcher["DepAnalysis"] = DepAnalysis;


            Func<CommMessage, CommMessage> OpenFile = msg =>
            {
                var send_path = string.Empty;
                var count = 0;

                try
                {
                    var FileToOpen = msg.arguments[0];


                    send_path = localFileMgr.pathStack.Peek();


                    comm.postFile(send_path, FileToOpen);

                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "OpenFile";
                    reply.arguments.Add(FileToOpen);

                    return reply;
                }
                catch (Exception e)
                {
                    var temp_list = new List<string> {""};
                    Console.WriteLine(e);
                    var reply = new CommMessage(CommMessage.MessageType.reply);
                    reply.to = msg.from;
                    reply.from = msg.to;
                    reply.command = "OpenFile";
                    reply.arguments = temp_list;
                    return reply;
                }
            };
            messageDispatcher["OpenFile"] = OpenFile;
        }


        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */
        private static void Main(string[] args)
        {
            TestUtilities.title("Starting Server", '=');
            Console.WriteLine("\n  Listening to Port: " + ServerEnvironment.port);
            try
            {
                var server = new NavigatorServer();
                server.initializeDispatcher();
                server.comm = new Comm(ServerEnvironment.address, ServerEnvironment.port);
                var directory = Directory.GetParent(Directory.GetParent(ServerEnvironment.root).ToString()).ToString();

                while (true)
                {
                    var msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    var reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                    if (msg.command == "DepAnalysis")
                    {
                        server.comm.postFile("Analysis.txt");
                        File.Delete(directory + "/Analysis.txt");
                    }

                    if (msg.command == "Close")
                    {
                        server.comm.close();

                        Process.GetCurrentProcess().Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}