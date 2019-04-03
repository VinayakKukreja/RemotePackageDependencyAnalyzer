///////////////////////////////////////////////////////////////////////
// Parser.cs - Parser detects code constructs defined by rules       //
// ver 1.5                                                           //
// Language:    C#, 2008, .Net Framework 4.0                         //
// Platform:    Dell Precision T7400, Win7, SP1                      //
// Application: Demonstration for CSE681, Project #2, Fall 2011      //
// Author:      Jim Fawcett, CST 4-187, Syracuse University          //
//              (315) 443-3948, jfawcett@twcny.rr.com                //
///////////////////////////////////////////////////////////////////////
/*
 * Module Operations:
 * ------------------
 * This module defines the following class:
 *   Parser  - a collection of IRules
 */
/* Required Files:
 *   IRulesAndActions.cs, RulesAndActions.cs, Parser.cs, Semi.cs, Toker.cs
 *   Display.cs
 *   
 * Maintenance History:
 * --------------------
 * ver 1.5 : 14 Oct 2014
 * - added bug fix to tokenizer to avoid endless loop on
 *   multi-line strings
 * ver 1.4 : 30 Sep 2014
 * - modified test stub to display scope counts
 * ver 1.3 : 24 Sep 2011
 * - Added exception handling for exceptions thrown while parsing.
 *   This was done because Toker now throws if it encounters a
 *   string containing @".
 * - RulesAndActions were modified to fix bugs reported recently
 * ver 1.2 : 20 Sep 2011
 * - removed old stack, now replaced by ScopeStack
 * ver 1.1 : 11 Sep 2011
 * - added comments to parse function
 * ver 1.0 : 28 Aug 2011
 * - first release
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lexer;

namespace CodeAnalysis
{
    /////////////////////////////////////////////////////////
    // rule-based parser used for code analysis

    public class Parser
    {
        private readonly List<IRule> Rules;

        public Parser()
        {
            Rules = new List<IRule>();
        }

        public void add(IRule rule)
        {
            Rules.Add(rule);
        }

        public void parse(ITokenCollection semi)
        {
            // Note: rule returns true to tell parser to stop
            //       processing the current semiExp

            Display.displaySemiString(semi.ToString());

            foreach (var rule in Rules)
                if (rule.test(semi))
                    break;
        }
    }

    internal class TestParser
    {
        //----< process commandline to get file references >-----------------

        private static List<string> ProcessCommandline(string[] args)
        {
            var files = new List<string>();
            if (args.Length == 0)
            {
                Console.Write("\n  Please enter file(s) to analyze\n\n");
                return files;
            }

            var path = args[0];
            path = Path.GetFullPath(path);
            for (var i = 1; i < args.Length; ++i)
            {
                var filename = Path.GetFileName(args[i]);
                files.AddRange(Directory.GetFiles(path, filename));
            }

            return files;
        }

        private static void ShowCommandLine(string[] args)
        {
            Console.Write("\n  Commandline args are:\n  ");
            foreach (var arg in args) Console.Write("  {0}", arg);
            Console.Write("\n  current directory: {0}", Directory.GetCurrentDirectory());
            Console.Write("\n");
        }

        //----< Test Stub >--------------------------------------------------

#if(TEST_PARSER)

        private static void Main(string[] args)
        {
            Console.Write("\n  Demonstrating Parser");
            Console.Write("\n ======================\n");

            ShowCommandLine(args);
            var files = ProcessCommandline(args);

            var repo = Repository.getInstance();
            var semi = Factory.create();
            var builder = new BuildTypeAnalyzer(semi);
            var parser = builder.build();

            foreach (var file in files)
            {
                Console.Write("\n  Processing file {0}\n", Path.GetFileName(file));
                repo.currentFile = file;

                //ITokenCollection semi = Factory.create();
                //semi.displayNewLines = false;
                if (!semi.open(file))
                {
                    Console.Write("\n  Can't open {0}\n\n", args[0]);
                    return;
                }

                Console.Write("\n  Type and Function Analysis");
                Console.Write("\n ----------------------------");

                //BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
                //Parser parser = builder.build();
                //repo.currentFile = file;

                try
                {
                    while (semi.get().Count > 0)
                        parser.parse(semi);
                }
                catch (Exception ex)
                {
                    Console.Write("\n\n  {0}\n", ex.Message);
                }

                var rep = Repository.getInstance();
                var table = rep.locations;
                Display.showMetricsTable(table);
                Console.Write("\n");

                semi.close();
            }

            Console.Write("\n\n");
        }
#endif
    }
}