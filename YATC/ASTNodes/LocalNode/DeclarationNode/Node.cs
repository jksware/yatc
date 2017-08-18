/*
 *  Yet Another Tiger Compiler (YATC)
 *
 *  Copyright 2014 Damian Valdés Santiago, Juan Carlos Pujol Mainegra
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in all
 *  copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *  SOFTWARE.  
 *
 */
using System.Collections.Generic;
using System.Linq;

namespace YATC.ASTNodes
{
    internal class Node<T>
    {
        public enum ColorCode
        {
            White = 0,
            Unvisited = 0,
            Black = 1,
            Done = 1,
            Gray = 2,
            StillNotDone = 2
        }

        public ColorCode Color { get; private set; }
        public Node<T> Parent { get; private set; }
        public readonly T Value;

        public Node(T value)
        {
            this.Value = value;
        }

        private readonly LinkedList<Node<T>> _pred = new LinkedList<Node<T>>();
        private readonly LinkedList<Node<T>> _succ = new LinkedList<Node<T>>();

        public IEnumerable<Node<T>> Pred { get { return _pred; } }
        public IEnumerable<Node<T>> Succ { get { return _succ; } }
        public IEnumerable<Node<T>> Adj { get { return _pred.Concat(_succ); } }

        public void AddSucc(Node<T> node) { _succ.AddLast(node); }
        public void AddPred(Node<T> node) { _pred.AddLast(node); }

        private static Node<T> DFS(IEnumerable<Node<T>> nodes, bool checkCycles, out LinkedList<Node<T>> linkedList)
        {
            int time = 0;
            linkedList = new LinkedList<Node<T>>();
            foreach (var node in nodes.Where(node => node.Color == ColorCode.White))
            {
                var cycleEnd = node.Visit(ref time, checkCycles, linkedList);
                if (cycleEnd != null)
                    return cycleEnd;
            }

            return null;
        }

        private Node<T> Visit(ref int time, bool checkCycles, LinkedList<Node<T>> topologicalSort)
        {
            this.Color = ColorCode.Gray;
            foreach (var node in this.Succ)
            {
                // by the Proof of Theorem 22.12, page 614, (u,v) is a back edge iff v is Gray
                // from Cormen et al. - Introduction To Algorithms, 3rd edition
                if (node.Color == ColorCode.Gray && checkCycles)
                    return this;

                if (node.Color == ColorCode.White)
                {
                    node.Parent = this;
                    var cycleEnd = node.Visit(ref time, checkCycles, topologicalSort);
                    if (cycleEnd != null)
                        return cycleEnd;
                }
            }
            this.Color = ColorCode.Black;
            topologicalSort.AddFirst(this);

            return null;
        }

        public static Node<T> TopologicalSort(IEnumerable<Node<T>> nodes, out LinkedList<Node<T>> linkedList)
        {
            return DFS(nodes, true, out linkedList);
        }

        public class NodeValueEqualityComparer : IEqualityComparer<Node<T>>
        {
            public bool Equals(Node<T> x, Node<T> y)
            {
                return x.Value.Equals(y.Value);
            }

            public int GetHashCode(Node<T> obj)
            {
                return obj.Value.GetHashCode();
            }
        }
    }
}