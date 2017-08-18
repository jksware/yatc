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
    class IfNode : AtomicNode
    {
        public IfNode(IToken payload)
            : base(payload)
        {
        }

        public ExpressionNode ConditionNode { get { return (ExpressionNode)TigerChildren[0]; } }
        public ExpressionNode ThenExpressionNode { get { return (ExpressionNode)TigerChildren[1]; } }
        public ExpressionNode ElseExpressionNode
        {
            get
            {
                return TigerChildren.Length > 2 ? (ExpressionNode)TigerChildren[2] : null;
            }
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.ConditionNode.CheckSemantics(scope, report);
            this.ThenExpressionNode.CheckSemantics(scope, report);
            bool allOk = this.ConditionNode.IsOk && this.ThenExpressionNode.IsOk;

            if (this.ConditionNode.IsOk)
            {
                if (this.ConditionNode.TigerType.Basetype == BaseType.Void)
                {
                    report.AddError(this.Line, this.Column,
                                    "Type mismatch: Invalid use of if condition with a non-valued expression.",
                                    this.ConditionNode.TigerType.Name);
                    this.TigerType = TigerType.Error;
                    allOk = false;
                }
                else
                    if (this.ConditionNode.TigerType.Basetype != BaseType.Int)
                    {
                        report.AddError(this.Line, this.Column,
                                        "Type mismatch: Invalid use of if condition with a non-int expression: '{0}' was found.",
                                        this.ConditionNode.TigerType.Name);
                        this.TigerType = TigerType.Error;
                        allOk = false;
                    }
            }

            TigerType returnType = this.ThenExpressionNode.TigerType;

            if (this.ElseExpressionNode != null)
            {
                this.ElseExpressionNode.CheckSemantics(scope, report);
                allOk &= this.ElseExpressionNode.IsOk;

                bool isThenAssignable =
                    this.ThenExpressionNode.TigerType.IsAssignableTo(this.ElseExpressionNode.TigerType);

                returnType = isThenAssignable ? this.ElseExpressionNode.TigerType : this.ThenExpressionNode.TigerType;

                /* facilita que se puede asignar record y nil, con independencia del orden en que aparezca */
                if (!isThenAssignable && !this.ElseExpressionNode.TigerType.IsAssignableTo(this.ThenExpressionNode.TigerType))
                {
                    report.AddError(this.Line, this.Column,
                        "Type mismatch: The then and else expression types of an if-then-else differ: " +
                        "'{0}' and '{1}' were found.",
                        this.ThenExpressionNode.TigerType.Name, this.ElseExpressionNode.TigerType.Name);
                    this.TigerType = TigerType.Error;
                    return;
                }
            }
            else
            {
                if (this.ThenExpressionNode.TigerType.Basetype != BaseType.Void)
                {
                    report.AddError(this.Line, this.Column,
                        "Type mismatch: The then expression at a if-then statement must not return a value: " +
                        "Found '{0}' whilst expecting void.",
                        this.ThenExpressionNode.TigerType.Name);
                    this.TigerType = TigerType.Error;
                    return;
                }
            }

            this.TigerType = allOk ? returnType : TigerType.Error;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.ConditionNode.GenerateCode(moduleBuilder);
            this.ThenExpressionNode.GenerateCode(moduleBuilder);

            if (this.ElseExpressionNode != null)
            {
                this.ElseExpressionNode.GenerateCode(moduleBuilder);

                this.VmExpression = Expression.Condition(
                    Expression.NotEqual(this.ConditionNode.VmExpression, Expression.Constant(0)),
                    this.ThenExpressionNode.VmExpression,
                    this.ElseExpressionNode.VmExpression);
            }
            else
            {
                this.VmExpression = Expression.IfThen(
                    Expression.NotEqual(this.ConditionNode.VmExpression, Expression.Constant(0)),
                    this.ThenExpressionNode.VmExpression);
            }
        }
    }
}
