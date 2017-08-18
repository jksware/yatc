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
    /// <summary>
    /// for id := expr_1 to expr_2 do expr_3;
    /// </summary>
    class ForNode : StatementNode, IBreakeableNode
    {
        public ForNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }
        public ExpressionNode FromExpression { get { return (ExpressionNode)TigerChildren[1]; } }
        public ExpressionNode ToExpression { get { return (ExpressionNode)TigerChildren[2]; } }
        public ExpressionNode DoExpression { get { return (ExpressionNode)TigerChildren[3]; } }

        public LabelTarget BreakTarget { get; set; }
        private VariableInfo _iterVarInfo;

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.FromExpression.CheckSemantics(scope, report);
            this.ToExpression.CheckSemantics(scope, report);

            TigerScope innerScope = scope.CreateChildScope();

            if (innerScope.CanFindFunVarInfo(this.IdNode.Name, false))
                report.AddWarning(this.Line, this.Column,
                    "Variable name hides outer scope variable or function: '{0}'.",
                    this.IdNode.Name);

            _iterVarInfo = new VariableInfo(this.IdNode.Name, new TigerTypeHolder(TigerType.Int), false) { IsReadOnly = true };
            innerScope.Add(_iterVarInfo);

            this.DoExpression.CheckSemantics(innerScope, report);

            if (!this.FromExpression.IsOk || !this.ToExpression.IsOk || !this.DoExpression.IsOk)
            {
                this.TigerType = TigerType.Error;
                return;
            }

            if (this.FromExpression.TigerType.Basetype != BaseType.Int ||
                this.ToExpression.TigerType.Basetype != BaseType.Int)
            {
                this.TigerType = TigerType.Error;
                report.AddError(this.Line, this.Column,
                    "Type mismatch: Expecting integer (or alias) type on intervals expression: '{0}' and '{1}' were found.",
                    this.FromExpression.TigerType.Name,
                    this.ToExpression.TigerType.Name);
                return;
            }

            if (this.DoExpression.TigerType.Basetype != BaseType.Void)
            {
                this.TigerType = TigerType.Error;
                report.AddError(this.Line, this.Column,
                    "Type mismatch: Expecting void return type in for expression: '{0}' was found.",
                    this.DoExpression.TigerType.Name);
                return;
            }

            this.TigerType = TigerType.Void;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            BreakTarget = Expression.Label();
            ParameterExpression iter = _iterVarInfo.ParameterExpression =
                 Expression.Parameter(typeof(int), "iter");
            ParameterExpression fromExpr = Expression.Parameter(typeof(int), "fromExpr");
            ParameterExpression toExpr = Expression.Parameter(typeof(int), "toExpr");

            this.FromExpression.GenerateCode(moduleBuilder);
            this.ToExpression.GenerateCode(moduleBuilder);
            this.DoExpression.GenerateCode(moduleBuilder);

            BlockExpression blockExpression = Expression.Block(
                new ParameterExpression[] { iter, fromExpr, toExpr },
                new Expression[]
                    {
                        Expression.Assign(fromExpr, this.FromExpression.VmExpression),
                        Expression.Assign(toExpr, this.ToExpression.VmExpression),
                        Expression.Assign(iter, fromExpr),
                        Expression.Loop(Expression.Block(
                            Expression.IfThen(Expression.GreaterThan(iter, toExpr),
                                Expression.Break(BreakTarget)),
                            this.DoExpression.VmExpression,
                            Expression.PostIncrementAssign(iter)
                        ), BreakTarget)
                    });

            this.VmExpression = blockExpression;
        }
    }
}
