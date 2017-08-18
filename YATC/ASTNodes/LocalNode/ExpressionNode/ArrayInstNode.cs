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
using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class ArrayInstNode : ExpressionNode
    {
        private ArrayType _arrayType;

        public ArrayInstNode(IToken payload)
            : base(payload)
        {
        }

        public IdNode IdNode { get { return (IdNode)TigerChildren[0]; } }
        public ExpressionNode IndexExpressionNode { get { return (ExpressionNode)TigerChildren[1]; } }
        public ExpressionNode OfExpressionNode { get { return (ExpressionNode)TigerChildren[2]; } }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            TigerTypeInfo arrayInfo = scope.FindTypeInfo(this.IdNode.Name, false);
            if (arrayInfo == null || arrayInfo.Holder == null || arrayInfo.Holder.TigerType == null)
            {
                report.AddError(this.Line, this.Column, "Undeclared array type: '{0}'.", this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            if (arrayInfo.Holder.TigerType.Basetype != BaseType.Array)
            {
                report.AddError(this.Line, this.Column, "Given type is not an array: {0}", this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            _arrayType = arrayInfo.Holder.TigerType as ArrayType;
            if (_arrayType == null)
                throw new NullReferenceException();

            this.IndexExpressionNode.CheckSemantics(scope, report);
            if (!this.IndexExpressionNode.IsOk)
            {
                this.TigerType = TigerType.Error;
                return;
            }

            if (this.IndexExpressionNode.TigerType.Basetype != BaseType.Int)
            {
                report.AddError(this.Line, this.Column, "Type mismatch: Given index expression is not an integer: {0}", this.IndexExpressionNode.TigerType.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            this.OfExpressionNode.CheckSemantics(scope, report);
            if (!this.OfExpressionNode.IsOk)
            {
                this.TigerType = TigerType.Error;
                return;
            }

            if (!this.OfExpressionNode.TigerType.IsAssignableTo(_arrayType.ElementTypeHolder.TigerType))
            {
                report.AddError(this.Line, this.Column, "Type mismatch: Array element and expression types: '{0}' and '{1}'",
                    _arrayType.ElementTypeHolder.TigerType.Name, this.OfExpressionNode.TigerType.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            this.TigerType = _arrayType;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.IndexExpressionNode.GenerateCode(moduleBuilder);
            this.OfExpressionNode.GenerateCode(moduleBuilder);

            Type elementType = _arrayType.ElementTypeHolder.TigerType.GetCLRType();
            Type arrayType = elementType.MakeArrayType();
            ParameterExpression arrayParamExpr = Expression.Parameter(arrayType);
            ParameterExpression counterExpr = Expression.Parameter(typeof(int));

            var arrayInitExpr = Expression.NewArrayBounds(elementType, this.IndexExpressionNode.VmExpression);

            LabelTarget breakLabel = Expression.Label();

            BlockExpression blockInitExpression = Expression.Block(
                new ParameterExpression[] { arrayParamExpr, counterExpr },
                new Expression[]
                    {
                        Expression.Assign(arrayParamExpr, arrayInitExpr),
                        Expression.Assign(counterExpr, Expression.Constant(0)),
                        Expression.Loop(
                            Expression.Block(
                                Expression.IfThenElse(
                                    Expression.LessThan(
                                        counterExpr, 
                                        this.IndexExpressionNode.VmExpression),                                         
                                    Expression.Assign(
                                        Expression.ArrayAccess(arrayParamExpr, counterExpr),
                                        Expression.Convert(
                                            this.OfExpressionNode.VmExpression,
                                            elementType
                                        )
                                    ),
                                    Expression.Break(breakLabel)
                                    ),
                                Expression.Assign(counterExpr, Expression.Increment(counterExpr))
                            ), breakLabel),
                        arrayParamExpr
                    }
            );

            this.VmExpression = blockInitExpression;
        }
    }
}
