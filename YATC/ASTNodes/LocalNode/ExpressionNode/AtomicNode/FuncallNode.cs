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

using System;
using Antlr.Runtime;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class FunCallNode : AtomicNode
    {
        public FunCallNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }

        public ExpressionNode[] ActualParametersNodes
        {
            get
            {
                return TigerChildren.Length > 1 ?
                    TigerChildren[1].TigerChildren.Cast<ExpressionNode>().ToArray() :
                    new ExpressionNode[] {};
            }
        }

        public FunctionInfo FunctionInfo { get; set; }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            FunctionInfo functionInfo = scope.FindFunctionInfo(this.IdNode.Name, false);
            this.FunctionInfo = functionInfo;

            if (functionInfo == null)
            {
                report.AddError(this.Line, this.Column, "Undeclared function: '{0}'.", this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            if (this.ActualParametersNodes == null && functionInfo.ParameterInfo.Length > 0 ||
                this.ActualParametersNodes != null && this.ActualParametersNodes.Length != functionInfo.ParameterInfo.Length)
            {
                report.AddError(this.Line, this.Column,
                    "Length of formal and actual parameters differ for function or procedure '{0}': " +
                    "Expecting {1} and found {2} arguments.",
                    this.IdNode.Name,
                    functionInfo.ParameterInfo.Length,
                    this.ActualParametersNodes != null ? this.ActualParametersNodes.Length : 0);
                this.TigerType = TigerType.Error;
                return;
            }

            if (this.ActualParametersNodes != null)
            {
                for (int i = 0; i < functionInfo.ParameterInfo.Length; i++)
                {
                    this.ActualParametersNodes[i].CheckSemantics(scope, report);
                    if (!this.ActualParametersNodes[i].IsOk)
                    {
                        this.TigerType = TigerType.Error;
                        return;
                    }

                    if (!this.ActualParametersNodes[i].TigerType.IsAssignableTo(functionInfo.ParameterInfo[i].Holder.TigerType))
                    {
                        report.AddError(this.Line, this.Column,
                                        "Types mismatch: Formal and actual parameter types differ for argument number {0} whilst calling " +
                                        "function or procedure '{3}': Expecting '{1}' and found '{2}'.",
                                        i,
                                        functionInfo.ParameterInfo[i].Holder.TigerType.Name,
                                        this.ActualParametersNodes[i].TigerType.Name,
                                        this.IdNode.Name);
                        this.TigerType = TigerType.Error;
                        return;
                    }
                }
            }

            this.TigerType = functionInfo.Holder.TigerType;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            Expression[] arguments = new Expression[this.ActualParametersNodes.Length];
            for (int i = 0; i < this.ActualParametersNodes.Length; i++)
            {
                ExpressionNode argument = this.ActualParametersNodes[i];
                argument.GenerateCode(moduleBuilder);

                arguments[i] = Expression.Convert(
                    argument.VmExpression,
                    this.FunctionInfo.ParameterInfo[i].Holder.TigerType.GetCLRType()
                );
            }

            if (this.FunctionInfo.Name == "exit")
            {
                var exit = ((Action<int>)Environment.Exit).Method;
                this.VmExpression = Expression.Call(exit, arguments[0]);
                return;
            }

            if (this.FunctionInfo.MethodInfo != null)
                this.VmExpression = Expression.Call(this.FunctionInfo.MethodInfo, arguments);
            else
                this.VmExpression = Expression.Invoke(this.FunctionInfo.LambdaExpression, arguments);
        }
    }
}
