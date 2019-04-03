///////////////////////////////////////////////////////////////////////////
// CsGraph.cs - Generic node and directed graph classes                  //
// ver 2.0                                                               //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Spring 2018     //
///////////////////////////////////////////////////////////////////////////
/*
 * Maintenance History:
 * --------------------
 * ver 2.0 : 29 Nov 2018
 * - added strong component analysis
 * ver 1.1 : 23 Aug 2018
 * - changed definition of CsNode<V,E>
 * - changed logic and return type of CsGraph<V,E>::walk
 * ver 1,0 : 18 Aug 2018
 * - first release
 */

using System;
using System.Collections.Generic;

namespace CsGraph
{
    /////////////////////////////////////////////////////////////////////////
    // CsEdge<V,E> and CsNode<V,E> classes

    public class CsEdge<V, E> // holds child node and instance of edge type E
    {
        public CsEdge(CsNode<V, E> node, E value)
        {
            targetNode = node;
            edgeValue = value;
        }

        public CsNode<V, E> targetNode { get; set; }
        public E edgeValue { get; set; }
    }

    public class CsNode<V, E>
    {
        //----< construct a named node >---------------------------------------

        public CsNode(string nodeName)
        {
            name = nodeName;
            children = new List<CsEdge<V, E>>();
            visited = false;
        }

        public V nodeValue { get; set; }
        public string name { get; set; }
        public List<CsEdge<V, E>> children { get; set; }
        public bool visited { get; set; }
        public int index { get; set; } = -1;
        public int lowlink { get; set; }

        public bool onStack { get; set; }
        //----< add child vertex and its associated edge value to vertex >-----

        public void addChild(CsNode<V, E> childNode, E edgeVal)
        {
            children.Add(new CsEdge<V, E>(childNode, edgeVal));
        }
        //----< find the next unvisited child >--------------------------------

        public CsEdge<V, E> getNextUnmarkedChild()
        {
            foreach (var child in children)
                if (!child.targetNode.visited)
                {
                    child.targetNode.visited = true;
                    return child;
                }

            return null;
        }
        //----< has unvisited child? >-----------------------------------

        public bool hasUnmarkedChild()
        {
            foreach (var child in children)
                if (!child.targetNode.visited)
                    return true;
            return false;
        }

        public void unmark()
        {
            visited = false;
        }

        public override string ToString()
        {
            return name;
        }
    }
    /////////////////////////////////////////////////////////////////////////
    // Operation<V,E> class

    public class Operation<V, E>
    {
        //----< graph.walk() calls this on every node >------------------------

        public virtual bool doNodeOp(CsNode<V, E> node)
        {
            Console.Write("\n  {0}", node);
            return true;
        }
        //----< graph calls this on every child visitation >-------------------

        public virtual bool doEdgeOp(E edgeVal)
        {
            Console.Write(" {0}", edgeVal.ToString());
            return true;
        }
    }
    /////////////////////////////////////////////////////////////////////////
    // CsGraph<V,E> class

    public class CsGraph<V, E>
    {
        private Operation<V, E> gop;

        //----< construct a named graph >--------------------------------------

        public CsGraph(string graphName)
        {
            name = graphName;
            adjList = new List<CsNode<V, E>>();
            gop = new Operation<V, E>();
            startNode = null;
        }

        public CsNode<V, E> startNode { get; set; }
        public string name { get; set; }
        public bool showBackTrack { get; set; }

        public Dictionary<int, List<CsNode<V, E>>> strongComp { get; set; }
            = new Dictionary<int, List<CsNode<V, E>>>();

        private int index { get; set; }
        private Stack<CsNode<V, E>> S { get; } = new Stack<CsNode<V, E>>();
        private int strongCompId { get; set; }

        public List<CsNode<V, E>> adjList { get; set; } // node adjacency list
        //----< register an Operation with the graph >-------------------------

        public Operation<V, E> setOperation(Operation<V, E> newOp)
        {
            var temp = gop;
            gop = newOp;
            return temp;
        }
        //----< add vertex to graph adjacency list >---------------------------

        public void addNode(CsNode<V, E> node)
        {
            adjList.Add(node);
        }
        //----< find vertex by name >------------------------------------------

        public int findNodeByName(string name)
        {
            for (var i = 0; i < adjList.Count; ++i)
                if (adjList[i].name == name)
                    return i;
            return -1;
        }
        //----< clear visitation marks to prepare for next walk >--------------

        public void clearMarks()
        {
            foreach (var node in adjList)
                node.unmark();
        }
        //----< depth first search from startNode >----------------------------

        public void walk()
        {
            if (adjList.Count == 0)
            {
                Console.Write("\n  no nodes in graph");
                return;
            }

            if (startNode == null)
            {
                Console.Write("\n  no starting node defined");
                return;
            }

            if (gop == null)
            {
                Console.Write("\n  no node or edge operation defined");
                return;
            }

            walk(startNode);
            foreach (var node in adjList)
                if (!node.visited)
                    walk(node);
            foreach (var node in adjList)
                node.unmark();
        }
        //----< depth first search from specific node >------------------------

        public void walk(CsNode<V, E> node)
        {
            // process this node

            gop.doNodeOp(node);
            node.visited = true;

            // visit children
            do
            {
                var childEdge = node.getNextUnmarkedChild();
                if (childEdge == null) return;

                gop.doEdgeOp(childEdge.edgeValue);
                walk(childEdge.targetNode);
                if (node.hasUnmarkedChild() || showBackTrack) gop.doNodeOp(node); // more edges to visit so announce
            } while (true);
        }

        private void strongConnect(CsNode<V, E> node)
        {
            node.index = index;
            node.lowlink = index;
            ++index;
            S.Push(node);
            node.onStack = true;

            CsNode<V, E> child = null;
            foreach (var edge in node.children)
            {
                child = edge.targetNode;
                if (child.index == -1)
                {
                    strongConnect(child);
                    node.lowlink = Math.Min(node.lowlink, child.lowlink);
                }
                else if (child.onStack)
                {
                    node.lowlink = Math.Min(node.lowlink, child.index);
                }
            }

            if (node.lowlink == node.index)
            {
                var compNodes = new List<CsNode<V, E>>();
                strongComp.Add(strongCompId, compNodes);
                do
                {
                    child = S.Pop();
                    child.onStack = false;
                    strongComp[strongCompId].Add(child);
                } while (child != node);

                ++strongCompId;
            }
        }

        public void strongComponents()
        {
            strongCompId = 0;
            index = 0;
            S.Clear();

            foreach (var node in adjList)
                if (node.index == -1)
                    strongConnect(node);
        }

        public void showDependencies()
        {
            Console.Write("\n  Dependency Table:");
            Console.Write("\n ------------------------------------------");
            foreach (var node in adjList)
            {
                Console.Write("\n  {0}", node.name);
                for (var i = 0; i < node.children.Count; ++i)
                    Console.Write("\n         {0}", node.children[i].targetNode.name);
            }
        }
    }
    /////////////////////////////////////////////////////////////////////////
    // Test class

    internal class demoOperation : Operation<string, string>
    {
        public override bool doNodeOp(CsNode<string, string> node)
        {
            Console.Write("\n -- {0}", node.name);
            return true;
        }
    }

    internal class Test
    {
        private static void Main(string[] args)
        {
            Console.Write("\n  Testing CsGraph class");
            Console.Write("\n =======================");

            var node1 = new CsNode<string, string>("node1");
            var node2 = new CsNode<string, string>("node2");
            var node3 = new CsNode<string, string>("node3");
            var node4 = new CsNode<string, string>("node4");
            var node5 = new CsNode<string, string>("node5");

            node1.addChild(node2, "edge12");
            node1.addChild(node3, "edge13");
            node2.addChild(node3, "edge23");
            node2.addChild(node4, "edge24");
            node3.addChild(node1, "edge31");
            node5.addChild(node1, "edge51");
            node5.addChild(node4, "edge54");

            var graph = new CsGraph<string, string>("Fred");
            graph.addNode(node1);
            graph.addNode(node2);
            graph.addNode(node3);
            graph.addNode(node4);
            graph.addNode(node5);

            graph.showDependencies();

            graph.startNode = node1;
            Console.Write("\n\n  starting walk at {0}", graph.startNode.name);
            Console.Write("\n  not showing backtracks");
            graph.walk();

            graph.startNode = node2;
            Console.Write("\n\n  starting walk at {0}", graph.startNode.name);
            graph.showBackTrack = true;
            Console.Write("\n  show backtracks");
            graph.setOperation(new demoOperation());
            graph.walk();

            Console.Write("\n\n  Strong Components:");
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