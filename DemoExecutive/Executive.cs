///////////////////////////////////////////////////////////////////////
// Executive.cs - Demonstrate Prototype Code Analyzer                //
// ver 2.0                                                           //
// Language:    C#, 2017, .Net Framework 4.7.1                       //
// Platform:    Dell Precision T8900, Win10                          //
// Application: Demonstration for CSE681, Project #3, Fall 2018      //
// Source:      Jim Fawcett, CST 4-187, Syracuse University          //
//              (315) 443-3948, jfawcett@twcny.rr.com                //
// Author: Vinayak Kukreja                                           //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines the class:
 *   Executive:
 *   - uses Parser, RulesAndActions, Semi, and Toker to perform type-based
 *     dependency analyzes
 */
/*
 *  Public Interface:
 * -------------------
 * Class Executive:
 *  public void typeAnalysis(List<string> files): build type table by parsing files for type defs
 *  public void dependencyAnalysis(List<string> files): build dependency table by parsing for type usage
 *  public CsGraph<string, string> buildDependencyGraph(): build dependency graph from dependency table
 *  public void getResult(List<string> files): Used to provide result of the analysis
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
 *   
 * Maintenance History:
 * --------------------
 * ver 2.0 : 29 Nov 2018
 * - added dependency and strong component analysis
 * ver 1.0 : 09 Oct 2018
 * - first release
 */

using System;
using System.Collections.Generic;
using System.IO;
using CsGraph;
using Lexer;
using Navigator;

namespace CodeAnalysis
{
    ///////////////////////////////////////////////////////////////////
    // Executive class
    // - finds files to analyze, using Navigate component
    // - builds typetable, in pass #1, by parsing files for defined types
    // - builds dependency table, in pass #2, by parsing files for:
    //   - type declarations, e.g., T t;, after stripping off modifiers
    //   - method parameter declarations, e.g., myFun(T t)
    //   - inheritance, e.g., class X : Y { ...
    //   and using typetable file and namespace info
    // - builds dependency graph from dependency table and analyzes 
    //   strong components

    public class Executive
    {
        private List<string> files { get; set; } = new List<string>();

        //----< process commandline to verify path >---------------------

        private bool ProcessCommandline(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Write("\n  Please enter path to analyze\n\n");
                return false;
            }

            var path = args[0];
            if (!Directory.Exists(path))
            {
                Console.Write("\n  invalid path \"{0}\"", Path.GetFullPath(path));
                return false;
            }

            return true;
        }
        //----< show arguments on command line >-------------------------

        private static void ShowCommandLine(string[] args)
        {
            Console.Write("\n  Commandline args are:\n  ");
            foreach (var arg in args) Console.Write("  {0}", arg);
            Console.Write("\n  current directory: {0}", Directory.GetCurrentDirectory());
            Console.Write("\n");
        }
        //----< build type table by parsing files for type defs >--------

        public void typeAnalysis(List<string> files)
        {
            Console.Write("\n  Type Analysis");
            Console.Write("\n ---------------------");

            Console.Write(
                "\n  {0,10}  {1,23}  {2,30}",
                "category", "name", "file"
            );
            Console.Write(
                "\n  {0,10}  {1,30}  {2,30}",
                "--------------", "---------------------", "-----------------------"
            );

            var semi = Factory.create();
            var builder = new BuildTypeAnalyzer(semi);
            var parser = builder.build();
            var repo = Repository.getInstance();

            foreach (var file in files)
            {
                if (file.Contains("TemporaryGeneratedFile"))
                    continue;
                if (!semi.open(file)) continue;

                repo.currentFile = file;
                repo.locations.Clear();

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
        }
        //----< build dependency table by parsing for type usage >-------

        public void dependencyAnalysis(List<string> files)
        {
            var repo = Repository.getInstance();
            var semi = Factory.create();
            var builder2 = new BuildDepAnalyzer(semi);
            var parser = builder2.build();
            repo.locations.Clear();

            foreach (var file in files)
            {
                //Console.Write("\n  file: {0}", file);
                if (file.Contains("TemporaryGeneratedFile") || file.Contains("AssemblyInfo"))
                    continue;

                if (!semi.open(file))
                {
                    Console.Write("\n  Can't open {0}\n\n", file);
                    break;
                }

                var deps = new List<string>();
                repo.dependencyTable.addParent(file);

                repo.currentFile = file;

                try
                {
                    while (semi.get().Count > 0)
                        //semi.show();
                        parser.parse(semi);
                }
                catch (Exception ex)
                {
                    Console.Write("\n\n  {0}\n", ex.Message);
                }
            }
        }
        //----< build dependency graph from dependency table >-----------

        public CsGraph<string, string> buildDependencyGraph()
        {
            var repo = Repository.getInstance();

            var graph = new CsGraph<string, string>("deps");
            foreach (var item in repo.dependencyTable.dependencies)
            {
                var fileName = item.Key;
                fileName = Path.GetFileName(fileName);

                var node = new CsNode<string, string>(fileName);
                graph.addNode(node);
            }

            var dt = new DependencyTable();
            foreach (var item in repo.dependencyTable.dependencies)
            {
                var fileName = item.Key;
                fileName = Path.GetFileName(fileName);
                if (!dt.dependencies.ContainsKey(fileName))
                {
                    var deps = new List<string>();
                    dt.dependencies.Add(fileName, deps);
                }

                foreach (var elem in item.Value)
                {
                    var childFile = elem;
                    childFile = Path.GetFileName(childFile);
                    dt.dependencies[fileName].Add(childFile);
                }
            }

            foreach (var item in graph.adjList)
            {
                var node = item;
                var children = dt.dependencies[node.name];
                foreach (var child in children)
                {
                    var index = graph.findNodeByName(child);
                    if (index != -1)
                    {
                        var dep = graph.adjList[index];
                        node.addChild(dep, "edge");
                    }
                }
            }

            return graph;
        }

        // Used to provide result of the analysis
        public void getResult(List<string> files)
        {
            var exec = new Executive();

            exec.typeAnalysis(files);


            Console.Write("\n  TypeTable Contents:");
            Console.Write("\n ----------------------------");

            var repo = Repository.getInstance();
            repo.typeTable.show();

            Console.WriteLine();
            Console.Write("\n  Dependency Analysis:");
            Console.Write("\n ------------------------------------------");


            exec.dependencyAnalysis(files);
            repo.dependencyTable.show();


            Console.Write("\n\n  Building Dependency Graph");
            Console.Write("\n ------------------------------------------");


            var graph = exec.buildDependencyGraph();
            graph.showDependencies();


            Console.Write("\n\n  Strong Components:");
            Console.Write("\n ------------------------------------------");


            graph.strongComponents();
            foreach (var item in graph.strongComp)
            {
                Console.Write("\n  component {0}", item.Key);
                Console.Write("\n    ");
                foreach (var elem in item.Value) Console.Write("     {0}", elem.name);
            }

            Console.Write("\n\n");
        }


        //----< processing starts here >---------------------------------

        private static void Main(string[] args)
        {
            Console.Write("\n  Dependency Analysis");
            Console.Write("\n =====================\n");

            var exec = new Executive();

            ShowCommandLine(args);

            // finding files to analyze

            var nav = new Navigate();
            nav.Add("*.cs");
            nav.go(args[0]); // read path from command line
            var files = nav.allFiles;

            exec.typeAnalysis(files);

            Console.Write("\n  TypeTable Contents:");
            Console.Write("\n ---------------------");

            var repo = Repository.getInstance();
            repo.typeTable.show();
            Console.Write("\n");

            /////////////////////////////////////////////////////////////////
            // Pass #2 - Find Dependencies

            Console.Write("\n  Dependency Analysis:");
            Console.Write("\n ----------------------");

            exec.dependencyAnalysis(files);
            repo.dependencyTable.show();

            Console.Write("\n\n  building dependency graph");
            Console.Write("\n ---------------------------");

            var graph = exec.buildDependencyGraph();
            graph.showDependencies();

            Console.Write("\n\n  Strong Components:");
            Console.Write("\n --------------------");
            graph.strongComponents();
            foreach (var item in graph.strongComp)
            {
                Console.Write("\n  component {0}", item.Key);
                Console.Write("\n    ");
                foreach (var elem in item.Value) Console.Write("{0} ", elem.name);
            }

            Console.Write("\n\n");
        }
    }
}