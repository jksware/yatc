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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class ExprSeqNode : AtomicNode
    {
        public ExprSeqNode(IToken payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Says whether the node contains a BreakNode, that is not within the control of a cycle control structure.
        /// </summary>
        public bool HasBreakInside { get; internal set; }

        public ExpressionNode[] ExpressionNodes
        {
            get { return TigerChildren.Length > 0 ? 
                    TigerChildren.Cast<ExpressionNode>().ToArray() : null; }
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            if (this.ExpressionNodes == null)
            {
                this.TigerType = TigerType.Void;
                return;
            }

            bool allOk = true;
            foreach (var expressionNode in this.ExpressionNodes)
            {
                expressionNode.CheckSemantics(scope, report);
                if (!expressionNode.IsOk)
                    allOk = false;
            }

            this.TigerType = allOk
                      ? ((this.HasBreakInside || ExpressionNodes.Length == 0)
                             ? TigerType.Void
                             : ExpressionNodes[ExpressionNodes.Length - 1].TigerType)
                      : TigerType.Error;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            if (this.ExpressionNodes != null)
            {
                foreach (var expressionNode in this.ExpressionNodes)
                    expressionNode.GenerateCode(moduleBuilder);

                this.VmExpression = Expression.Block(new ParameterExpression[] { },
                                                     this.ExpressionNodes.Select(x => x.VmExpression ?? Expression.Empty()));
            }
            else
                this.VmExpression = Expression.Empty();
        }
    }
}