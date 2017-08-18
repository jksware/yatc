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

using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace YATC.Scope
{
    public abstract class TigerNode : CommonTree
    {
        protected TigerNode(IToken payload)
            : base(payload)
        {
        }

        public bool IsRoot
        {
            get { return Parent == null; }
        }

        /// <summary>
        /// Lists all the nodes from current to root
        /// </summary>
        public IEnumerable<TigerNode> GetNodesToRoot()
        {
            if (this.IsRoot)
                yield break;

            Debug.Assert(this.Parent is TigerNode);

            var parent = Parent as TigerNode;
            yield return parent;
            foreach (var item in parent.GetNodesToRoot())
                yield return item;
        }

        public int Column { get { return CharPositionInLine; } }

        //public new TigerNode Parent
        //{
        //    get { return (TigerNode)base.Parent; }
        //}

        public TigerNode[] TigerChildren
        {
            get
            {
                return base.Children == null ? new TigerNode[0] : base.Children.Cast<TigerNode>().ToArray();
            }
        }
    }
}
