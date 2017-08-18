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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class FunDeclNode : DeclarationNode
    {
        public FunDeclNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }
        public TypeFieldNode[] Params
        {
            get { return TigerChildren[1].TigerChildren.Cast<TypeFieldNode>().ToArray(); }
        }
        public TypeNode TypeNode
        {
            get { return TigerChildren[2].TigerChildren.Length > 0 ?
                    (TypeNode)TigerChildren[2].TigerChildren[0] :
                    null; }
        }
        public ExpressionNode ExpressionBodyNode { get { return (ExpressionNode)TigerChildren[3]; } }

        public string Name { get { return this.IdNode.Name; } }
        public FunctionInfo FunctionInfo;

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            FunctionInfo = scope.FindFunctionInfo(this.Name, true);

            if (FunctionInfo == null)
                throw new NullReferenceException();

            var innerScope = scope.CreateChildScope();
            foreach (var parameterInfo in FunctionInfo.ParameterInfo)
            {
                if (innerScope.CanFindFunVarInfo(parameterInfo.Name, false))
                    report.AddWarning(this.Line, this.Column, 
                        "Parameter name hides outer scope variable or function in '{0}': '{1}'.",
                        this.Name, parameterInfo.Name);
                innerScope.Add(parameterInfo);
            }

            this.ExpressionBodyNode.CheckSemantics(innerScope, report);

            // chequeando que el tipo de retorno sea el mismo que el tipo de la expresion,
            // no se hace salvedad en el caso de los procedures.
            if (!this.ExpressionBodyNode.TigerType.IsAssignableTo(FunctionInfo.Holder.TigerType))
            {
                report.AddError(this.Line, this.Column,
                    "Type mismatch: Function or procedure return and expression types in '{0}': " +
                    "Expecting '{1}' and '{2}' found.",
                    this.Name, FunctionInfo.Holder.TigerType.Name, this.ExpressionBodyNode.TigerType.Name);
                this.IsOK = false;
                return;
            }

            this.IsOK = true;
        }

        public Type DelegateType;

        internal ParameterExpression GenerateHeaderCode()
        {
            bool funRets = FunctionInfo.Holder.TigerType.Basetype != BaseType.Void;

            Type[] paramTypes = new Type[this.FunctionInfo.ParameterInfo.Length + (funRets ? 1 : 0)];
            for (int i = 0; i < this.FunctionInfo.ParameterInfo.Length; i++)
                paramTypes[i] = this.FunctionInfo.ParameterInfo[i].Holder.TigerType.GetCLRType();

            if (funRets)
                paramTypes[this.Params.Length] = this.FunctionInfo.Holder.TigerType.GetCLRType();

            DelegateType = funRets
                                ? Expression.GetFuncType(paramTypes)
                                : Expression.GetActionType(paramTypes);

            var paramExpr = Expression.Parameter(DelegateType);

            FunctionInfo.LambdaExpression = paramExpr;
            return paramExpr;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            var paramsExprs = new ParameterExpression[this.Params.Length];
            for (int i = 0; i < Params.Length; i++)
            {
                paramsExprs[i] = Expression.Parameter(this.FunctionInfo.ParameterInfo[i].Holder.TigerType.GetCLRType());
                FunctionInfo.ParameterInfo[i].ParameterExpression = paramsExprs[i];
            }

            this.ExpressionBodyNode.GenerateCode(moduleBuilder);

            Expression bodyExpr = this.FunctionInfo.Holder.TigerType.Basetype == BaseType.Void
                                      ? Expression.Block(
                                            this.ExpressionBodyNode.VmExpression, 
                                            Expression.Empty()
                                        )
                                      : (Expression)Expression.Convert(
                                            ExpressionBodyNode.VmExpression,
                                            this.FunctionInfo.Holder.TigerType.GetCLRType()
                                        );

            this.VmExpression = Expression.Lambda(
                //DelegateType,
                bodyExpr,
                this.Name,
                paramsExprs
            );
        }
    }
}
