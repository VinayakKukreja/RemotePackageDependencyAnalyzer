///////////////////////////////////////////////////////////////////////
///  NavigateWithDelegates.cs - Navigates a Directory Subtree,      ///
///  ver 1.1       displaying files and some of their properties    ///
///                                                                 ///
///  Language:     Visual C#                                        ///
///  Platform:     Dell Dimension 8100, Windows Pro 2000, SP2       ///
///  Application:  CSE681 Example                                   ///
///  Author:       Jim Fawcett, CST 2-187, Syracuse Univ.           ///
///                (315) 443-3948, jfawcett@twcny.rr.com            ///
///////////////////////////////////////////////////////////////////////
/*
 *  Module Operations:
 *  ==================
 *  Recursively displays the contents of a directory tree
 *  rooted at a specified path, with specified file pattern.
 *
 *  This version uses delegates to avoid embedding application
 *  details in the Navigator.  Navigate now is reusable.  Clients
 *  simply register event handlers for Navigate events newDir 
 *  and newFile.
 * 
 *  Public Interface:
 *  =================
 *  Navigate nav = new Navigate();
 *  nav.go("c:\temp","*.cs");
 *  nav.newDir += new Navigate.newDirHandler(OnDir);
 *  nav.newFile += new Navigate.newFileHandler(OnFile);
 * 
 *  Maintenance History:
 *  ====================
 *  ver 1.3 : 19 Aug 2018
 *  - converted public delegate (common practice) to a
 *    public property with private backing store
 *  ver 1.2 : 09 Aug 2018
 *  - added multiple pattern handling and modified comments
 *  ver 1.1 : 04 Sep 2006
 *  - added file pattern as argument to member function go()
 *  ver 1.0 : 25 Sep 2003
 *  - first release
 */
//

/////////////////////////////////////////////////////////////////////
// FileMgr - provides file and directory handling for navigation   //
// ver 1.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines IFileMgr interface, FileMgrFactory, and LocalFileMgr
 * classes.  Clients use the FileMgrFactory to create an instance bound to
 * an interface reference.
 * 
 * The FileManager finds files and folders at the root path and in any
 * subdirectory in the tree rooted at that path.
 * 
 * Maintenance History:
 * --------------------
 * ver 1.1 : 23 Oct 2017
 * - moved all Environment definitions into an Environment project
 * ver 1.0 : 22 Oct 2017
 * - first release
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Navigator
{
    public enum FileMgrType
    {
        Local,
        Remote
    }

    ///////////////////////////////////////////////////////////////////
    // NavigatorClient uses only this interface and factory

    public interface IFileMgr
    {
        Stack<string> pathStack { get; set; }
        string currentPath { get; set; }
        IEnumerable<string> getFiles();
        IEnumerable<string> getFiles(string path);
        IEnumerable<string> getDirs();
        IEnumerable<string> getDirs(string path);
        bool setDir(string dir);
    }

    public class FileMgrFactory
    {
        public static IFileMgr create(FileMgrType type)
        {
            if (type == FileMgrType.Local)
                return new LocalFileMgr();
            return null; // eventually will have remote file Mgr
        }
    }

    ///////////////////////////////////////////////////////////////////
    // Concrete class for managing local files

    public class LocalFileMgr : IFileMgr
    {
        public LocalFileMgr()
        {
            pathStack.Push(currentPath); // stack is used to move to parent directory
        }

        public string currentPath { get; set; } = "";

        public Stack<string> pathStack { get; set; } = new Stack<string>();
        //----< get names of all files in current directory >------------


        public IEnumerable<string> getFiles()
        {
            var files = new List<string>();
            var path = Path.Combine(Environment.root, currentPath);
            var absPath = Path.GetFullPath(path);
            files = Directory.GetFiles(path).ToList();
            for (var i = 0; i < files.Count(); ++i)
                //files[i] = Path.Combine(currentPath, Path.GetFileName(files[i]));
                files[i] = Path.GetFileName(files[i]);
            return files;
        }


        //----< get names of all files in current directory >------------

        public IEnumerable<string> getFiles(string path)
        {
            var files = new List<string>();
            //string path = Path.Combine(Environment.root, currentPath);
            var absPath = Path.GetFullPath(path);
            files = Directory.GetFiles(path).ToList();
            for (var i = 0; i < files.Count(); ++i)
                //files[i] = Path.Combine(currentPath, Path.GetFileName(files[i]));
                files[i] = Path.GetFileName(files[i]);
            return files;
        }


        //----< get names of all subdirectories in current directory >---

        public IEnumerable<string> getDirs()
        {
            var dirs = new List<string>();
            var path = Path.Combine(Environment.root, currentPath);
            dirs = Directory.GetDirectories(path).ToList();
            for (var i = 0; i < dirs.Count(); ++i)
            {
                var dirName = new DirectoryInfo(dirs[i]).Name;
                //dirs[i] = Path.Combine(currentPath, dirName);
                dirs[i] = dirName;
            }

            return dirs;
        }


        //----< get names of all subdirectories in current directory >---

        public IEnumerable<string> getDirs(string path)
        {
            var dirs = new List<string>();
            //string path = Path.Combine(Environment.root, currentPath);
            dirs = Directory.GetDirectories(path).ToList();
            for (var i = 0; i < dirs.Count(); ++i)
            {
                var dirName = new DirectoryInfo(dirs[i]).Name;
                //dirs[i] = Path.Combine(currentPath, dirName);
                dirs[i] = dirName;
            }

            return dirs;
        }


        //----< sets value of current directory - not used >-------------


        public bool setDir(string dir)
        {
            if (!Directory.Exists(dir))
                return false;
            currentPath = dir;
            return true;
        }
    }

    ///////////////////////////////////////////////////////////////////
    // Navigate class
    // - uses public event properties to avoid binding directly
    //   to application processing

    public class Navigate
    {
        // define public delegate type
        public delegate void newDirHandler(string dir);

        public delegate void newFileHandler(string file);

        private readonly List<string> patterns_ = new List<string>();

        public Navigate()
        {
            patterns_.Add("*.*");
        }

        public List<string> allFiles { get; set; } = new List<string>();

        public newDirHandler newDir // public delegate property
        {
            get => newDir_;
// getter's logic can be changed without
            // changing user interface
            set => newDir_ = value;
// same for setter
        }

        public newFileHandler newFile
        {
            get => newFile_;
            set => newFile_ = value;
        }

        // define public delegate property with private backing store
        private event newDirHandler newDir_; // private backing store

        private event newFileHandler newFile_;

        public void Add(string pattern)
        {
            if (patterns_.Count == 1 && patterns_[0] == "*.*") patterns_.Clear();
            patterns_.Add(pattern);
        }
        ///////////////////////
        // The go function has no application specific code.
        // It just invokes its event delegate to notify clients.
        // The clients take care of all the application specific stuff.

        public void go(string path, bool recurse = true)
        {
            path = Path.GetFullPath(path);

            //List<string> allFiles = new List<string>();

            foreach (var patt in patterns_)
            {
                var files = Directory.GetFiles(path, patt);
                allFiles.AddRange(files);
            }

            if (newDir != null && allFiles.Count > 0)
                newDir.Invoke(path);

            foreach (var file in allFiles)
                if (newFile != null)
                    newFile.Invoke(Path.GetFileName(file));

            if (recurse)
            {
                var dirs = Directory.GetDirectories(path);
                foreach (var dir in dirs)
                    go(dir);
            }
        }

        public void go(string path, List<string> listOfSelectedFiles)
        {
            path = Path.GetFullPath(path);

            //List<string> allFiles = new List<string>();

            foreach (var patt in patterns_)
            {
                var files = Directory.GetFiles(path, patt);
                foreach (var file in files)
                    if (listOfSelectedFiles.Contains(Path.GetFileName(file)))
                        allFiles.Add(file);
            }

            if (newDir != null && allFiles.Count > 0)
                newDir.Invoke(path);

            //foreach (string file in allFiles)
            //{
            //      //if (newFile != null)
            //      //{
            //      //    if (listOfSelectedFiles.Contains(Path.GetFileName(file)))
            //      //    {
            //      //        newFile.Invoke(Path.GetFileName(file));
            //      //    }
            //      //}
            //    string s = Path.GetFileName(file);
            //    if (listOfSelectedFiles.Contains(s))
            //    {
            //        newFile.Invoke(Path.GetFileName(file));
            //    }

            //  }

            //if (recurse)
            //{
            //    string[] dirs = Directory.GetDirectories(path);
            //    foreach (string dir in dirs)
            //        go(dir);
            //}
        }
    }
}