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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    internal class FunDeclSeqNode : DeclarationNode
    {
        public FunDeclSeqNode(IToken payload)
            : base(payload)
        {
        }

        public FunDeclNode[] FunDeclNodes { get { return TigerChildren.Cast<FunDeclNode>().ToArray(); } }

        public ParameterExpression[] FunctionClousures { get; private set; }
        public Expression[] FunctionAssigns { get; private set; }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            for (int index = 0; index < FunDeclNodes.Length; index++)
            {
                var funDeclNode = FunDeclNodes[index];

                if (scope.CanFindFunVarInfo(funDeclNode.Name, true))
                {
                    report.AddError(this.Line, this.Column,
                                    "Redeclared local function or variable name: '{0}'.", funDeclNode.Name);
                    this.IsOK = false;
                    return;
                }

                FunctionInfo outerFunction = scope.FindFunctionInfo(funDeclNode.Name, false);
                if (outerFunction != null)
                    if (outerFunction.IsStandard)
                    {
                        report.AddError(this.Line, this.Column,
                                        "Redeclared standard function or procedure: '{0}'.", funDeclNode.Name);
                        this.IsOK = false;
                        return;
                    }
                    else
                        report.AddWarning(this.Line, this.Column,
                                          "Function name hides outer scope variable or function: '{0}'.",
                                          funDeclNode.Name);

                // Checking not repiting parameter names
                var parameterNames = new HashSet<string>();
                var parameterInfo = new VariableInfo[funDeclNode.Params.Length];
                for (int i = 0; i < parameterInfo.Length; i++)
                {
                    var parameter = funDeclNode.Params[i];
                    if (!parameterNames.Add(parameter.Name))
                    {
                        report.AddError(this.Line, this.Column,
                                        "Redeclared function or procedure formal parameter name: '{0}'.", parameter.Name);
                        this.IsOK = false;
                        return;
                    }

                    parameter.TypeNode.CheckSemantics(scope, report);
                    TigerTypeInfo paramInfo = scope.FindTypeInfo(parameter.TypeNode.Name, false);

                    if (paramInfo == null)
                    {
                        report.AddError(this.Line, this.Column,
                                        "Undeclared type: Function or procedure parameter: " +
                                        "'{0}' of formal parameter '{1}'.", parameter.TypeNode.Name, parameter.Name);
                        this.IsOK = false;
                        return;
                    }

                    parameterInfo[i] = new VariableInfo(parameter.Name, paramInfo.Holder, true);
                }

                // Checking return type
                TigerType returnType;
                if (funDeclNode.TypeNode != null)
                {
                    TigerTypeInfo returnInfo = scope.FindTypeInfo(funDeclNode.TypeNode.Name, false);
                    if (returnInfo == null)
                    {
                        report.AddError(this.Line, this.Column,
                                        "Undeclared function or procedure return type: {0}", funDeclNode.TypeNode.Name);
                        this.IsOK = false;
                        return;
                    }
                    returnType = returnInfo.Holder.TigerType;
                }
                else
                    returnType = TigerType.Void;

                var functionInfo = new FunctionInfo(funDeclNode.Name, parameterInfo, new TigerTypeHolder(returnType), false);
                scope.Add(functionInfo);
            }

            // Checking children
            bool areAllOk = true;
            foreach (var funDeclNode in FunDeclNodes)
            {
                funDeclNode.CheckSemantics(scope, report);
                if (!funDeclNode.IsOK)
                    areAllOk = false;
            }

            this.IsOK = areAllOk;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            FunctionClousures = new ParameterExpression[this.FunDeclNodes.Length];
            FunctionAssigns = new Expression[this.FunDeclNodes.Length];
            for (int i = 0; i < this.FunDeclNodes.Length; i++)
                FunctionClousures[i] = this.FunDeclNodes[i].GenerateHeaderCode();

            for (int i = 0; i < this.FunDeclNodes.Length; i++)
                this.FunDeclNodes[i].GenerateCode(moduleBuilder);

            for (int i = 0; i < this.FunDeclNodes.Length; i++)
                FunctionAssigns[i] = Expression.Assign(
                    this.FunDeclNodes[i].FunctionInfo.LambdaExpression,
                    this.FunDeclNodes[i].VmExpression
                );
        }
    }
}