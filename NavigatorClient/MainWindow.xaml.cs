////////////////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - Demonstrates the GUI for the Client Side             //
// ver 2.0                                                                  //
// Source :Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 //
// Author:  Vinayak Kukreja                                               //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines WPF application processing by the client.  The client
 * displays a navigation view and helps to choose where to start the analysis from
 * 
 * Required Files:
 * ----------------
 * CodePopUp.xaml, CodePopUp.xaml.cs, Server.cs, MPCommService.cs, BlockingQueue.cs, IMPCommService.cs, Environment.cs
 *
 * Public Interface:
 * ------------------
 * Class MainWindow:
 *  public MainWindow(): Constructor- Initializing Components here and setting up the environment
 *
 *
 * Maintenance History:
 * --------------------
 * Source:
 *
 * ver 2.1 : 26 Oct 2017
 * - relatively minor modifications to the Comm channel used to send messages
 *   between NavigatorClient and NavigatorServer
 * ver 2.0 : 24 Oct 2017
 * - added remote processing - Up functionality not yet implemented
 *   - defined NavigatorServer
 *   - added the CsCommMessagePassing prototype
 * ver 1.0 : 22 Oct 2017
 * - first release
 *
 * Author:
 * ver 1.0:
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using MessagePassingComm;

namespace Navigator
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, Action<CommMessage>> messageDispatcher =
            new Dictionary<string, Action<CommMessage>>();

        private readonly Thread rcvThread;
        private int enableButton = 0;
        private string receiving_file_path = string.Empty;
        private bool unselecting = false;

        //Constructor- Initializing Components here and setting up the environment
        public MainWindow()
        {
            InitializeComponent();
            initializeEnvironment();

            Console.Title = "Client";
            fileMgr = FileMgrFactory.create(FileMgrType.Local); // uses Environment
            //getTopFiles();
            comm = new Comm(ClientEnvironment.address, ClientEnvironment.port);
            initializeMessageDispatcher();

            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
            try
            {
                AutomaticTestSuite();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public string path { get; set; }
        private List<string> selectedFiles { get; set; } = new List<string>();


        private IFileMgr fileMgr { get; }
        private Comm comm { get; }


        //----< make Environment equivalent to ClientEnvironment >-------

        private void initializeEnvironment()
        {
            Environment.root = ClientEnvironment.root;
            Environment.address = ClientEnvironment.address;
            Environment.port = ClientEnvironment.port;
            Environment.endPoint = ClientEnvironment.endPoint;
        }


        //----< define how to process each message command >-------------

        private void initializeMessageDispatcher()
        {
            messageDispatcher["connect"] = msg =>
            {
                ServerResult.Text = msg.arguments[0];
                ClientResult.Text = msg.arguments[0];
            };


            messageDispatcher["OpenFile"] = msg =>
            {
                try
                {
                    var path = Path.Combine(ClientEnvironment.root, msg.arguments[0]);
                    var contents = File.ReadAllText(path);
                    var popup = new CodePopUp();
                    popup.codeView.Text = contents;
                    popup.Show();
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            messageDispatcher["getTopFiles"] = msg =>
            {
                remoteFiles.Items.Clear();
                foreach (var file in msg.arguments) remoteFiles.Items.Add(file);
            };

            messageDispatcher["getUpFiles"] = msg =>
            {
                remoteFiles.Items.Clear();
                foreach (var file in msg.arguments) remoteFiles.Items.Add(file);
            };

            messageDispatcher["getTopDirs"] = msg =>
            {
                remoteDirs.Items.Clear();
                foreach (var dir in msg.arguments) remoteDirs.Items.Add(dir);
            };

            messageDispatcher["getUpDirs"] = msg =>
            {
                remoteDirs.Items.Clear();
                foreach (var dir in msg.arguments) remoteDirs.Items.Add(dir);
            };

            messageDispatcher["DepAnalysis"] = msg => { receiving_file_path = msg.arguments[0]; };

            messageDispatcher["moveIntoFolderFiles"] = msg =>
            {
                remoteFiles.Items.Clear();
                foreach (var file in msg.arguments) remoteFiles.Items.Add(file);
            };

            messageDispatcher["moveIntoFolderDirs"] = msg =>
            {
                remoteDirs.Items.Clear();
                foreach (var dir in msg.arguments) remoteDirs.Items.Add(dir);
            };
        }


        //----< define processing for GUI's receive thread >-------------

        private void rcvThreadProc()
        {
            Console.Write("\n  Starting client's receive thread \n");
            Console.WriteLine("\n  Listening to Port: " + ClientEnvironment.port);
            while (true)
            {
                var msg = comm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                Dispatcher.Invoke(messageDispatcher[msg.command], msg);
            }
        }


        //----< shut down comm when the main window closes >-------------

        private void Window_Closed(object sender, EventArgs e)
        {
            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Vinayak Kukreja";
            msg1.command = "Close";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            comm.close();

            Process.GetCurrentProcess().Kill();
        }

        // This function is for getting the top directory and files in the GUI
        private void RemoteTop_ClickAuto()
        {
            UpButton.IsEnabled = true;
            AnalButton.IsEnabled = true;
            remoteDirs.IsEnabled = true;
            remoteFiles.IsEnabled = true;
            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Vinayak Kukreja";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            var msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
        }
        // Actual method where invocation of above method takes place
        private void RemoteTop_Click(object sender, RoutedEventArgs e)
        {
            RemoteTop_ClickAuto();
        }

        //----< download file and display source in popup window >-------

        private void remoteFiles_MouseDoubleClickAuto()
        {
            var selectedFilesList = remoteFiles.SelectedItems;
            var temp = string.Empty;
            var temp_list = new List<string>();
            foreach (string file in selectedFilesList)
            {
                temp = file;
                temp_list.Add(temp);
            }

            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Vinayak Kukreja";
            msg1.command = "OpenFile";
            msg1.arguments.Add(temp_list[0]);
            comm.postMessage(msg1);
        }

        // Actual method where invocation of above method takes place
        private void remoteFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            remoteFiles_MouseDoubleClickAuto();
        }


        //----< move to parent directory of current remote path >--------
        private void RemoteUp_ClickAuto()
        {
            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Vinayak Kukreja";
            msg1.command = "getUpFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            var msg2 = msg1.clone();
            msg2.command = "getUpDirs";
            comm.postMessage(msg2);
        }

        // Actual method where invocation of above method takes place
        private void RemoteUp_Click(object sender, RoutedEventArgs e)
        {
            RemoteUp_ClickAuto();
        }

        // Used to connect to server
        private void ConnectButton_ClickAuto()
        {
            TopButton.IsEnabled = true;
            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Vinayak Kukreja";
            msg1.command = "connect";
            msg1.arguments.Add("Connected");
            comm.postMessage(msg1);
            ConnectButton.IsEnabled = false;
        }

        // Actual method where invocation of above method takes place
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectButton_ClickAuto();
        }

        //----< move into remote subdir and display files and subdirs >--
        /*
         * - sends messages to server to get files and dirs from folder
         * - recv thread will create Action<CommMessage>s for the UI thread
         *   to invoke to load the remoteFiles and remoteDirs listboxs
         */

        private void remoteDirs_MouseDoubleClickAuto()
        {
            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(remoteDirs.SelectedValue as string);
            comm.postMessage(msg1);
            var msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
        }

        // Actual method where invocation of above method takes place
        private void remoteDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            remoteDirs_MouseDoubleClickAuto();
        }

        // Used to display the features provided by the GUI
        private void Info_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = "Information.txt";
                var path = Path.Combine(ClientEnvironment.root, fileName);

                if (File.Exists(path)) File.Delete(path);

                var temp = new List<string>
                {
                    "1. \t Connect Button lets you connect to the Server. \n",
                    "2. \t Click Get Top Files and Folders to reach the root directory of Server Repository. \n",
                    "3. \t Click Up to go a level up from the current directory. \n",
                    "4. \t Click Analyze to analyze the current directory .cs files as well as all the .cs files in the sub-directories of the current directory. \n",
                    "5. \t Analysis Result Button would only be activated when you click Analyze button. \n",
                    "6. \t Analysis Results tab is only selectable by clicking on Analysis Result Button. \n",
                    "7. \t You can also select a group of files by pressing and holding down Ctrl or Shift for analysis of specific files. If no files are selected, the default analysis takes place. \n",
                    "8. \t Connect Button would be deactivated if once clicked. Since introducing automated test suite, it is deactivated on opening. \n"
                };
                using (TextWriter tw = new StreamWriter(path))
                {
                    foreach (var s in temp)
                        tw.WriteLine(s);
                }


                using (var sr = File.OpenText(path))
                {
                    var contents = File.ReadAllText(path);
                    var popup = new CodePopUp();
                    popup.codeView.Text = contents;
                    popup.Show();
                }

                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }

        // This method sends the chosen parameters for dependency analysis
        private void AnalButton_ClickAuto()
        {
            var selectedFilesList = remoteFiles.SelectedItems;
            var temp = string.Empty;
            var temp_list = new List<string>();
            foreach (string file in selectedFilesList)
            {
                temp = file;
                temp_list.Add(temp);
            }

            AnalResButton.IsEnabled = true;
            var msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = ClientEnvironment.endPoint;
            msg1.to = ServerEnvironment.endPoint;
            msg1.author = "Vinayak Kukreja";
            msg1.command = "DepAnalysis";
            msg1.arguments = temp_list;
            comm.postMessage(msg1);
        }

        // Actual method where invocation of above method takes place
        private void AnalButton_Click(object sender, RoutedEventArgs e)
        {
            AnalButton_ClickAuto();
        }

        // This enables to view the result of the analysis.
        private void AnalButtonResult_ClickAuto()
        {
            if (!receiving_file_path.Equals(string.Empty))
            {
                var local_path = ClientEnvironment.root + receiving_file_path;
                AnalResult.Text = string.Empty;
                //var test_size = File.ReadAllBytes(local_path);
                var temp_text = File.ReadAllText(local_path);

                AnalResult.Text = File.ReadAllText(local_path);
                Console.Write(temp_text);
                File.Delete(local_path);
                tabs.SelectedIndex = 1;
                AnalResButton.IsEnabled = false;
            }
        }

        // Actual method where invocation of above method takes place
        private void AnalButtonResult_Click(object sender, RoutedEventArgs e)
        {
            AnalButtonResult_ClickAuto();
        }

        // This enables to perform operations when opened initially for demo purposes
        private void AutomaticTestSuite()
        {
            ConnectButton_ClickAuto();
            Thread.Sleep(500);
            RemoteTop_ClickAuto();
            Thread.Sleep(500);
            AnalButton_ClickAuto();
            Thread.Sleep(500);
            AnalButtonResult_ClickAuto();
            Thread.Sleep(500);
        }
    }
}