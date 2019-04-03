///////////////////////////////////////////////////////////////////////////
// IRuleAndAction.cs - Interfaces & abstract bases for rules and actions //
// ver 1.1                                                               //
// Language:    C#, 2008, .Net Framework 4.0                             //
// Platform:    Dell Precision T7400, Win7, SP1                          //
// Application: Demonstration for CSE681, Project #2, Fall 2011          //
// Author:      Jim Fawcett, CST 4-187, Syracuse University              //
//              (315) 443-3948, jfawcett@twcny.rr.com                    //
///////////////////////////////////////////////////////////////////////////
/*
 * Module Operations:
 * ------------------
 * This module defines the following classes:
 *   IRule   - interface contract for Rules
 *   ARule   - abstract base class for Rules that defines some common ops
 *   IAction - interface contract for rule actions
 *   AAction - abstract base class for actions that defines common ops
 */
/* Required Files:
 *   IRuleAndAction.cs
 *   
 * Build command:
 *   Interfaces and abstract base classes only so no build
 *   
 * Maintenance History:
 * --------------------
 * ver 1.1 : 11 Sep 2011
 * - added properties displaySemi and displayStack
 * ver 1.0 : 28 Aug 2011
 * - first release
 *
 * Note:
 * This package does not have a test stub as it contains only interfaces
 * and abstract classes.
 *
 */

using System;
using System.Collections.Generic;
using Lexer;

namespace CodeAnalysis
{
    /////////////////////////////////////////////////////////
    // contract for actions used by parser rules

    public interface IAction
    {
        void doAction(ITokenCollection semi);
    }
    /////////////////////////////////////////////////////////
    // abstract action base supplying common functions

    public abstract class AAction : IAction
    {
        public static Action<string> actionDelegate;

        protected Repository repo_;

        public static bool displaySemi { get; set; } = false;

        public static bool displayStack { get; set; } = false;

        public abstract void doAction(ITokenCollection semi);

        //void dummy(Elem elem) { }

        public virtual void display(ITokenCollection semi)
        {
            if (displaySemi)
                for (var i = 0; i < semi.size(); ++i)
                    Console.Write("{0} ", semi[i]);
        }
    }
    /////////////////////////////////////////////////////////
    // contract for parser rules

    public interface IRule
    {
        bool test(ITokenCollection semi);
        void add(IAction action);
    }
    /////////////////////////////////////////////////////////
    // abstract rule base implementing common functions

    public abstract class ARule : IRule
    {
        public static Action<string> actionDelegate;
        private readonly List<IAction> actions;

        public ARule()
        {
            actions = new List<IAction>();
        }

        public void add(IAction action)
        {
            actions.Add(action);
        }

        public abstract bool test(ITokenCollection semi);

        public void doActions(ITokenCollection semi)
        {
            foreach (var action in actions)
                action.doAction(semi);
        }

        public int indexOfType(ITokenCollection semi)
        {
            int indexCL;
            semi.find("class", out indexCL);
            int indexIF;
            semi.find("interface", out indexIF);
            int indexST;
            semi.find("struct", out indexST);
            int indexEN;
            semi.find("enum", out indexEN);
            int indexDE;
            semi.find("delegate", out indexDE);

            var index = Math.Max(indexCL, indexIF);
            index = Math.Max(index, indexST);
            index = Math.Max(index, indexEN);
            index = Math.Max(index, indexDE);
            return index;
        }
    }
}