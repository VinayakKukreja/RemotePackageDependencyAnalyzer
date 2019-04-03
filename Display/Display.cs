///////////////////////////////////////////////////////////////////////////
// Display.cs  -  Manage Display properties                              //
// ver 1.1                                                               //
// Language:    C#, Visual Studio 2013, .Net Framework 4.5               //
// Platform:    Dell XPS 2720 , Win 8.1 Pro                              //
// Application: Pr#2 Help, CSE681, Fall 2014                             //
// Author:      Jim Fawcett, CST 2-187, Syracuse University              //
//              (315) 443-3948, jfawcett@twcny.rr.com                    //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations
 * ==================
 * Display manages static public properties used to control what is displayed and
 * provides static helper functions to send information to MainWindow and Console.
 * 
 * Public Interface
 * ================
 * Display.showConsole = false;  // disables most writing to console
 * Display.showFooter = true;    // enables status information display in footer
 * ...
 * Display.displayRules(act, ruleStr)  // sends ruleStr to console and/or footer
 * ...
 */
/*
 * Build Process
 * =============
 * Required Files:
 *   FileMgr.cs
 *   
 * Compiler Command:
 *   devenv CSharp_Analyzer /rebuild debug
 * 
 * Maintenance History
 * ===================
 * ver 1.1 : 09 Oct 2018
 * - removed non-essential items from display
 * ver 1.0 : 19 Oct 2014
 *   - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CodeAnalysis
{
    ///////////////////////////////////////////////////////////////////
    // StringExt static class
    // - extension method to truncate strings

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }

    public static class Display
    {
        static Display()
        {
            showFiles = false;
            showDirectories = false;
            showActions = false;
            showRules = false;
            useFooter = false;
            useConsole = false;
            goSlow = false;
            width = 33;
        }

        public static bool showFiles { get; set; }
        public static bool showDirectories { get; set; }
        public static bool showActions { get; set; }
        public static bool showRules { get; set; }
        public static bool showSemi { get; set; }
        public static bool useFooter { get; set; }
        public static bool useConsole { get; set; }
        public static bool goSlow { get; set; }
        public static int width { get; set; }

        //----< display results of Code Analysis >-----------------------

        public static void showMetricsTable(List<Elem> table)
        {
            //Console.Write(
            //    "\n  {0,10}  {1,25}  {2,25}",
            //    "category", "name", "file"
            //    //"\n  {0,10}  {1,25}  {2,5}  {3,5}  {4,5}  {5,5}",
            //    //"category", "name", "bLine", "eLine", "size", "cmplx"
            //);
            //Console.Write(
            //    "\n  {0,10}  {1,25}  {2,25}",
            //    "--------", "----", "----"
            //    //"\n  {0,10}  {1,25}  {2,5}  {3,5}  {4,5}  {5,5}",
            //    //"--------", "----", "-----", "-----", "----", "-----"
            //);
            foreach (var e in table)
            {
                /////////////////////////////////////////////////////////
                // Uncomment to leave a space before each defined type
                // if (e.type == "class" || e.type == "struct")
                //   Console.Write("\n");

                var fileName = Path.GetFileName(e.file);
                Console.Write(
                    "\n  {0,10}  {1,25}  {2,25}",
                    e.type, e.name, fileName
                    //e.beginLine, e.endLine,
                    //e.endLine - e.beginLine + 1, e.endScopeCount - e.beginScopeCount + 1
                );
            }
        }
        //----< display a semiexpression on Console >--------------------

        public static void displaySemiString(string semi)
        {
            if (showSemi && useConsole)
            {
                Console.Write("\n");
                var sb = new StringBuilder();
                for (var i = 0; i < semi.Length; ++i)
                    if (!semi[i].Equals('\n'))
                        sb.Append(semi[i]);
                Console.Write("\n  {0}", sb);
            }
        }
        //----< display, possibly truncated, string >--------------------

        public static void displayString(Action<string> act, string str)
        {
            if (goSlow) Thread.Sleep(200); //  here only to support visualization
            if (act != null && useFooter)
                act.Invoke(str.Truncate(width));
            if (useConsole)
                Console.Write("\n  {0}", str);
        }
        //----< display string, possibly overriding client pref >--------

        public static void displayString(string str, bool force = false)
        {
            if (useConsole || force)
                Console.Write("\n  {0}", str);
        }
        //----< display rules messages >---------------------------------

        public static void displayRules(Action<string> act, string msg)
        {
            if (showRules) displayString(act, msg);
        }
        //----< display actions messages >-------------------------------

        public static void displayActions(Action<string> act, string msg)
        {
            if (showActions) displayString(act, msg);
        }
        //----< display filename >---------------------------------------

        public static void displayFiles(Action<string> act, string file)
        {
            if (showFiles) displayString(act, file);
        }
        //----< display directory >--------------------------------------

        public static void displayDirectory(Action<string> act, string file)
        {
            if (showDirectories) displayString(act, file);
        }

#if(TEST_DISPLAY)
        private static void Main(string[] args)
        {
            Console.Write("\n  Tested by use in Parser\n\n");
        }
#endif
    }
}