/////////////////////////////////////////////////////////////////////
// ReqsTests.cs - Test classes for Project2_InstrSol               //
// ver 1.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2018 //
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using Lexer;
using Navigator;

namespace CodeAnalysis
{
    using Token = String;

    internal class FileUtils
    {
        public static bool openFile(string fileSpec, out StreamReader sr)
        {
            sr = File.OpenText(fileSpec);
            return sr != null;
        }

        public static bool fileLines(string fileSpec, int start = 0, int end = 10000)
        {
            fileSpec = Path.GetFullPath(fileSpec);
            Console.Write("\n  file: \"{0}\"", fileSpec);
            StreamReader sr = null;
            try
            {
                sr = File.OpenText(fileSpec);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }

            var count = 0;
            string line;
            while (count < end)
            {
                line = sr.ReadLine();
                if (line == null)
                    return count > 0;
                if (++count > start)
                    Console.Write("\n  {0}", line);
            }

            return true;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // ReqDisplay class
    // - display methods for Requirements testing

    internal class ReqDisplay
    {
        public static void title(string tle)
        {
            Console.Write("\n  {0}", tle);
            Console.Write("\n {0}", new string('-', tle.Length + 2));
        }

        public static void message(string msg)
        {
            Console.Write("\n  {0}\n", msg);
        }

        public static void showSet(HashSet<string> set, string msg = "")
        {
            if (msg.Length > 0)
                Console.Write("\n  {0}\n  ", msg);
            else
                Console.Write("\n  Set:\n  ");
            foreach (var tok in set) Console.Write("\"{0}\" ", tok);
            Console.Write("\n");
        }

        public static void showList(List<string> lst, string msg = "")
        {
            if (msg.Length > 0)
                Console.Write("\n  {0}\n  ", msg);
            else
                Console.Write("\n  List:\n  ");
            var count = 0;
            foreach (var tok in lst)
            {
                Console.Write("\"{0}\" ", tok);
                if (++count == 10)
                {
                    count = 0;
                    Console.Write("\n  ");
                }
            }

            Console.Write("\n");
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Finder class
    // - finds semiExp with specified sequence of tokens in specified file

    internal class Finder
    {
        public static string file { get; set; } = "";

        public static bool findSequence(bool findAll, params string[] toks)
        {
            var found = false;
            if (!File.Exists(file))
                return false;
            var semi = new Semi();
            var toker = new Toker();
            toker.open(file);
            semi.toker = toker;
            while (!semi.isDone())
            {
                semi.get();
                if (semi.hasSequence(toks))
                {
                    semi.show();
                    found = true;
                    if (findAll == false)
                        return true;
                }
            }

            return found;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq3 class

    internal class TestReq3 : ITest
    {
        public string path { get; set; } = "../../";
        public string name { get; set; } = "Test Req3";
        public bool result { get; set; }

        public bool doTest()
        {
            ReqDisplay.title("Requirement #3");
            ReqDisplay.message("C# packages: Toker, SemiExp, ITokenCollection");
            var nav = new Navigate();
            nav.Add("*.cs");
            nav.newDir += onDir;
            nav.newFile += onFile;
            path = "../../../Toker";
            nav.go(path, false);
            path = "../../../SemiExp";
            nav.go(path, false);
            return result;
        }

        private void onFile(string filename)
        {
            Console.Write("\n    {0}", filename);
            result = true;
        }

        private void onDir(string dirname)
        {
            Console.Write("\n  {0}", dirname);
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq4 class

    internal class TestReq4 : ITest
    {
        public string fileSpec { get; set; } = "../../../Toker/Toker.cs";
        public string name { get; set; } = "Test Req4";
        public bool result { get; set; }

        public bool doTest()
        {
            ReqDisplay.title("Requirement #4");
            ReqDisplay.message("Toker implements state pattern");
            Finder.file = fileSpec;
            string[] toks = {"class", "{"};
            result = Finder.findSequence(true, toks);
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq5 class

    internal class TestReq5 : ITest
    {
        public string fileSpec { get; set; } = "../../../Toker/Test.txt";
        public string name { get; set; } = "Test Req5";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #5");
            ReqDisplay.message("Toker reads one token with each call to getTok()");
            var toker = new Toker();
            fileSpec = Path.GetFullPath(fileSpec);

            if (!toker.open(fileSpec))
            {
                Console.Write("\n  Toker can't open file \"{0}\"", fileSpec);
                return result = false;
            }

            Console.Write("\n  tokenizing file \"{0}\"", fileSpec);
            for (var i = 0; i < 5; ++i) Console.Write("\n  called Toker.getTok() to get \"{0}\"", toker.getTok());
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq6 class

    internal class TestReq6 : ITest
    {
        public string fileSpec { get; set; } = "../../../SemiExp/Test.txt";
        public string name { get; set; } = "Test Req6";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #6");
            ReqDisplay.message("Semi uses to get tokens until a terminator is retrieved");
            var toker = new Toker();
            fileSpec = Path.GetFullPath(fileSpec);
            if (!toker.open(fileSpec))
            {
                Console.Write("\n  toker can't open \"{0}\"", fileSpec);
                return result = false;
            }

            Console.Write("\n  processing file \"{0}\"", fileSpec);
            var semi = new Semi();
            semi.toker = toker;
            while (!semi.isDone())
            {
                semi.get();
                semi.show();
            }

            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq7 class

    internal class TestReq7 : ITest
    {
        public string name { get; set; } = "Test Req7";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #7");
            ReqDisplay.message("Semi terminators are \"{\", \"}\", \";\", \"\\n\" when first tok is \"#\"");

            Console.Write("\n  demonstrated by the output of Req6 test");
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq8 class

    internal class TestReq8 : ITest
    {
        public string name { get; set; } = "Test Req8";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #8");
            ReqDisplay.message("Semi folds for loops");

            Console.Write("\n  demonstrated by the output of Req6 test");
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq9 class

    internal class TestReq9 : ITest
    {
        public string fileSpec { get; set; } = "../../../SemiExp/Semi.cs";
        public string name { get; set; } = "Test Req9";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #9");
            ReqDisplay.message("Semi implements ITokenCollection");

            FileUtils.fileLines(fileSpec, 73, 75);
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq10a class

    internal class TestReq10a : ITest
    {
        public string fileSpec1 { get; set; } = "../../../Toker/Test2.txt";
        public string fileSpec2 { get; set; } = "../../../Semi/Test2.txt";
        public string name { get; set; } = "Test Req10a";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #10a");
            ReqDisplay.message("Testing special tokens");

            var toker = new Toker();
            var oneCharToks = toker.oneCharTokens();
            ReqDisplay.showSet(oneCharToks, "one char Tokens:");
            Console.Write("\n  adding token \"@\"");
            toker.addOneCharToken("@");
            ReqDisplay.showSet(oneCharToks, "one char Tokens:");
            Console.Write("\n  removing token \"@\"");
            toker.removeOneCharToken("@");
            ReqDisplay.showSet(oneCharToks, "one char Tokens:");
            var twoCharToks = toker.twoCharTokens();
            ReqDisplay.showSet(twoCharToks, "two char Tokens:");
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq10b class

    internal class TestReq10b : ITest
    {
        public string fileSpec1 { get; set; } = "../../../Toker/Test2.txt";
        public string fileSpec2 { get; set; } = "../../../Semi/Test2.txt";
        public string name { get; set; } = "Test Req10b";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #10b");
            ReqDisplay.message("Testing token extraction");

            result = FileUtils.fileLines(fileSpec1);
            if (!result)
                return false;

            var toker = new Toker();
            toker.doReturnComments = true;
            toker.open(fileSpec1);
            var tokList = new List<string>();
            while (!toker.isDone())
            {
                var tok = toker.getTok();
                if (tok == "\n")
                    tok = "\\n";
                if (tok == "\r")
                    tok = "\\r";
                tokList.Add(tok);
            }

            ReqDisplay.showList(tokList, "Tokens:");
            return result;
        }
    }
    ///////////////////////////////////////////////////////////////////
    // TestReq10c class

    internal class TestReq10c : ITest
    {
        public string fileSpec1 { get; set; } = "../../../Toker/Test2.txt";
        public string fileSpec2 { get; set; } = "../../../SemiExp/Test2.txt";
        public string name { get; set; } = "Test Req10c";
        public bool result { get; set; } = true;

        public bool doTest()
        {
            ReqDisplay.title("Requirement #10c");
            ReqDisplay.message("Testing semi extraction");

            result = FileUtils.fileLines(fileSpec2);
            if (!result)
                return false;

            var toker = new Toker();
            toker.doReturnComments = true;
            toker.open(fileSpec2);
            var semi = new Semi();
            semi.toker = toker;

            while (!semi.isDone())
            {
                semi.get();
                replace(semi, "\n", "\\n");
                replace(semi, "\r", "\\r");
                //replace(semi, )
                semi.show();
            }

            return result;
        }

        public void replace(Semi semi, string toGo, string toPut)
        {
            for (var i = 0; i < semi.size(); ++i)
                if (semi[i] == toGo)
                    semi[i] = toPut;
        }
    }
}