/////////////////////////////////////////////////////////////////////
// Semi.cs - Collects groups of tokens that are useful for         //
// ver 1.0   grammatical analysis                                  //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2018 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * - This package contains a Semi class that implements ITokenCollection
 *   interface.  It also contains a Factory class that creates instances
 *   of Semi.
 * - Semiexpressions are collections of tokens that are useful for 
 *   detecting specific grammatical constructs.  It is important that
 *   each instance of Semi contains all tokens necessary to make a 
 *   decision about a grammatical construct, e.g., is this a class?
 * - It is also important that each Semi instance does not contain 
 *   tokens relevant for more than one detection.
 * - This code demonstrates how to build a semi-expression collector,
 *   using the state-based tokenizer. 
 * - This Instructor's solution meets all requirements of Project #2
 * 
 * Required Files:
 * ---------------
 * Semi.cs
 * Toker.cs
 * 
 * Maintenance History
 * -------------------
 * ver 1.2 : 29 Nov 2018
 * - added methods:
 *   - hasSequence
 *   - getFunctionParams
 *   - clone
 * ver 1.1 : 05 Oct 2018
 * - modified only comments
 * ver 1.0 : 11 Sep 2018
 * - first release
 * 
 * Note:
 * - This is a redesign of the Semi package you will find in the Parser folder
 *   in the Repository.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lexer
{
    using Token = String;
    using SbToken = StringBuilder;
    using TokColl = List<string>;

    ///////////////////////////////////////////////////////////////////
    // Semi class
    // - collects tokens from Lexer::Toker.
    // - terminates collection on collecting the special tokens:
    //     ";", "{", "}", and "\n" when the first character is "#"
    // - It implements the IEnumerable interface, which is a contract
    //   to return an Enumerater object that foreach uses to step through
    //   items in a collection.
    // - It also implements the ITokenCollection interface that the
    //   parser will use to extract token sequences for rule-checking.

    public static class Factory
    {
        public static ITokenCollection create()
        {
            var rtn = new Semi();
            rtn.toker = new Toker();
            return rtn;
        }
    }

    public class Semi : ITokenCollection
    {
        private TokColl toks = new TokColl(); // private collection of tokens

        public Semi()
        {
        }

        public Semi(List<string> list)
        {
            toks.AddRange(list);
        }

        public Toker toker { get; set; } = new Toker();

        public bool open(string source)
        {
            return toker.open(source);
        }

        public void close()
        {
            toker.close();
        }
        //----< return number of tokens in semi-expression >-------------

        public int size()
        {
            return toks.Count;
        }
        //----< make copy of semi >--------------------------------------

        public ITokenCollection clone()
        {
            var theClone = new Semi();
            foreach (var token in toks)
                theClone.add(token);
            return theClone;
        }
        //----< convert semi to string >---------------------------------

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var tok in toks) sb.Append(tok).Append(" ");
            return sb.ToString();
        }
        //----< return number of lines processed >-----------------------

        public int lineCount()
        {
            return toker.lineCount();
        }
        //----< does semi contain specific token? >----------------------

        public bool contains(string tok)
        {
            if (toks.Contains(tok))
                return true;
            return false;
        }
        //----< find token in semi >-------------------------------------

        public bool find(string tok, out int index)
        {
            for (index = 0; index < size(); ++index)
                if (toks[index] == tok)
                    return true;
            index = -1;
            return false;
        }
        //----< find predecessor of token >------------------------------

        public string predecessor(string tok)
        {
            int index;
            if (find(tok, out index) && index > 0) return toks[index - 1];
            return "";
        }
        //----< test for ordered sequence of tokens >--------------------

        public bool hasSequence(params string[] tokSeq)
        {
            var position = 0;
            foreach (var tok in toks)
            {
                if (position == tokSeq.Length - 1)
                    return true;
                if (tok == tokSeq[position])
                    ++position;
            }

            return position == tokSeq.Length - 1;
        }
        //----< extract contiguous subset of tokens >--------------------

        public ITokenCollection getFunctionParams()
        {
            var subset = new Semi();
            int start, end;
            if (find("(", out start))
            {
                if (!find(")", out end))
                    return subset;
            }
            else
            {
                return subset;
            }

            for (var i = start + 1; i < end; ++i) subset.add(toks[i]);
            subset.add(";");
            return subset;
        }
        //----< used by parser to get the next collection of tokens >----

        public TokColl get()
        {
            toks.Clear();

            while (!toker.isDone())
            {
                var tok = toker.getTok();
                if (tok != "\n")
                    toks.Add(tok);
                if (isTerminator(tok))
                {
                    fold();
                    return toks;
                }
            }

            return toks;
        }
        //----< indexer allows us to index for a specific token >--------

        public string this[int i]
        {
            get => toks[i];
            set => toks[i] = value;
        }
        //----< function returning an enumerator >-----------------------

        public IEnumerator<string> GetEnumerator()
        {
            return toks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        //----< add a token to the end of this semi-expression >---------

        public ITokenCollection add(string token)
        {
            toks.Add(token);
            return this;
        }
        //----< remove a token >-----------------------------------------

        public bool remove(int n)
        {
            if (n < 0 || toks.Count <= n)
                return false;
            toks.RemoveAt(n);
            return true;
        }
        //----< clear all the tokens from internal collection >----------

        public void clear()
        {
            toks.Clear();
        }
        //----< are we at the end of the token source stream? >----------

        public bool isDone()
        {
            return toker.isDone();
        }
        //----< insert a token at position n >---------------------------

        public bool insert(int n, string tok)
        {
            if (n < 0 || n >= tok.Length)
                return false;
            toks.Insert(n, tok);
            return true;
        }
        //----< does this semi-expression contain a terminator? >--------

        public bool hasTerminator()
        {
            if (toks.Count <= 0)
                return false;
            if (isTerminator(toks[toks.Count - 1]))
                return true;
            return false;
        }
        //----< display contents of semi-expression on console >---------

        public void show()
        {
            Console.Write("\n-- ");
            foreach (var tok in toks)
                if (tok != "\n")
                    Console.Write("{0} ", tok);
                else
                    Console.Write("{0} ", "\\n");
        }

        public void addRange(ITokenCollection coll)
        {
            foreach (var tok in coll)
                toks.Add(tok);
        }
        //----< is tok a terminator for the current semi-expression? >---

        private bool isTerminator(string tok)
        {
            if (tok == ";" || tok == "{" || tok == "}")
                return true;
            if (tok == "\n")
            {
                trim();
                if (toks.Count > 0 && toks[0] == "#")
                    return true;
            }

            return false;
        }
        //----< remove leading newlines >--------------------------------

        private void trim()
        {
            var count = 0;
            for (count = 0; count < toks.Count; ++count)
                if (toks[count] != "\n")
                    break;
            if (count == 0)
                return;
            for (var i = 0; i < count; ++i)
                toks.RemoveAt(0);
        }
        //----< return last token in collection >------------------------

        public string last()
        {
            return toks[toks.Count - 1];
        }
        //----< fold >---------------------------------------------------

        private void fold()
        {
            if (hasSequence("for", "(", ";"))
            {
                var temp = new Semi(toks);
                get(); // get i<N;
                temp.addRange(this);
                get();
                temp.addRange(this); // get ++i) .. {
                toks = temp.toks;
            }
        }
    }

#if (TEST_SEMI)

    internal class TestSemi
    {
        private static void Main(string[] args)
        {
            Console.Write("\n  testing Semi");
            Console.Write("\n ==============");

            // Access Semi through interface and object factory.
            // That isolates client from any changes that may occur to Semi
            // as long as ITokenCollection doesn't change.

            var semi = Factory.create();

            var source = "../../semi.cs";
            if (!semi.open(source))
            {
                Console.Write("\n  Can't open {0}\n", source);
                return;
            }

            while (!semi.isDone())
            {
                semi.get();
                semi.show();
            }

            Console.Write("\n");

            Console.Write("\n  demonstrating semi operations");
            Console.Write("\n -------------------------------");

            var test = Factory.create();

            test.add("one").add("two").add("three");
            test.show();
            if (test.hasSequence("one", "three"))
                Console.Write("\n  semi has token \"one\" followed by token \"three\"");
            if (!test.hasSequence("foo", "two"))
                Console.Write("\n  semi does not have token \"foo\" followed by token \"two\"");
            if (!test.hasTerminator())
                Console.Write("\n  semi does not have terminator");

            Console.Write("\n  demonstrate changing semi with insert and add");
            test.insert(0, "#");
            test.add("\n");
            test.show();

            Console.Write("\n  demonstrate semi tests");
            if (test.hasTerminator())
                Console.Write("\n  semi has terminator");
            else
                Console.Write("\n  semi does not have terminator");

            int index;
            var tok = "two";
            if (test.find(tok, out index))
                Console.Write("\n  found token \"{0}\" at position {1}", tok, index);
            else
                Console.Write("\n  did not find token \"{0}\"", tok);
            tok = "foo";
            if (test.find(tok, out index))
                Console.Write("\n  found token \"{0}\" at position {1}", tok, index);
            else
                Console.Write("\n  did not find token \"{0}\"", tok);

            tok = "one";
            var tok2 = test.predecessor(tok);
            Console.Write("\n  predecessor of \"{0}\" is \"{1}\"", tok, tok2);
            tok = "bar";
            tok2 = test.predecessor(tok);
            Console.Write("\n  predecessor of \"{0}\" is \"{1}\"", tok, tok2);

            Console.Write("\n  indexing semi\n  ");
            for (var i = 0; i < test.size(); ++i)
                Console.Write("{0} ", test[i]);
            Console.Write("\n  using foreach:\n  ");
            foreach (var tk in test)
                Console.Write("{0} ", tk);
            Console.Write("\n");

            Console.Write("\n  testing subset");
            Console.Write("\n ----------------");

            var test2 = Factory.create();
            test2.add("void").add("someFun").add("(").add("Lexer").add(".");
            test2.add("Semi").add("semi").add(")").add("{").add("}");
            test2.show();
            var subset = test2.getFunctionParams();
            subset.show();
            Console.Write("\n\n");
        }
    }
}

#endif