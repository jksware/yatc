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
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class BreakNode : StatementNode
    {
        public BreakNode(IToken payload)
            : base(payload)
        {
        }

        public IBreakeableNode Owner { get; set; }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            foreach (var node in this.GetNodesToRoot())
            {
                var exprSeq = node as ExprSeqNode;
                if (exprSeq != null)
                    exprSeq.HasBreakInside = true;

                var breakable = node as IBreakeableNode;
                if (breakable != null)
                {
                    this.Owner = breakable;
                    break;
                }

                if (node is FunDeclNode)
                {
                    report.AddError(this.Line, this.Column,
                        "Break loop control structure not found within function.");
                    break;
                }
            }

            if (this.Owner == null)
            {
                report.AddError(Line, Column, "Break does not have a matching loop control structure owner.");
                this.TigerType = TigerType.Error;
                return;
            }

            this.TigerType = TigerType.Void;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.VmExpression = Expression.Break(this.Owner.BreakTarget);
        }
    }
}
