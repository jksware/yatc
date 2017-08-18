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
    class VarDeclNode : DeclarationNode
    {
        public VarDeclNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }
        public TypeNode TypeNode { get { return (TypeNode)TigerChildren[1]; } }
        public ExpressionNode RightExpressionNode { get { return (ExpressionNode)TigerChildren[2]; } }

        public VariableInfo VariableInfo { get; set; }
        public bool IsAutoVariable { get { return this.TypeNode is FillInTypeNode; } }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            string name = this.IdNode.Name;

            if (scope.CanFindFunVarInfo(name, true))
            {
                report.AddError(this.Line, this.Column,
                    "Redeclared local variable or function: '{0}'.",
                    name);
                this.IsOK = false;
                return;
            }

            FunctionInfo outerfunctionInfo = scope.FindFunctionInfo(name, false);
            if (outerfunctionInfo != null)
            {
                if (outerfunctionInfo.IsStandard)
                {
                    report.AddError(this.Line, this.Column,
                        "Cannot define variable name with standard function: '{0}'.",
                        name);
                    this.IsOK = false;
                    return;
                }
                else
                    report.AddWarning(this.Line, this.Column,
                        "Variable name hides outer scope variable or function: '{0}'.",
                        name);
            }
            this.RightExpressionNode.CheckSemantics(scope, report);

            if (!this.RightExpressionNode.IsOk)
            {
                this.IsOK = false;
                return;
            }

            if (this.RightExpressionNode.TigerType.Equals(TigerType.Void))
            {
                report.AddError(this.Line, this.Column, "Right hand side expression must evaluate to a returning value.");
                this.IsOK = false;
                return;
            }

            this.TypeNode.CheckSemantics(scope, report);
            TigerType returnType = this.RightExpressionNode.TigerType;

            if (!this.IsAutoVariable)
            {
                TigerTypeInfo tigerTypeInfo = scope.FindTypeInfo(this.TypeNode.Name, false);
                if (tigerTypeInfo == null)
                {
                    report.AddError(this.Line, this.Column, "Undeclared type: '{0}'.", this.TypeNode.Name);
                    this.IsOK = false;
                    return;
                }

                /* Assignment */
                if (!this.RightExpressionNode.TigerType.IsAssignableTo(tigerTypeInfo.Holder.TigerType))
                {
                    report.AddError(this.Line, this.Column,
                                    "Type mismatch: Variable declaration and expression do not match " +
                                    "for '{0}': expecting '{1}' and '{2}' found.",
                                    name, tigerTypeInfo.Holder.TigerType.Name, this.RightExpressionNode.TigerType.Name);
                    this.IsOK = false;
                    return;
                }

                returnType = tigerTypeInfo.Holder.TigerType;
            }
            else
            {
                if (returnType.Basetype == BaseType.Nil)
                {
                    report.AddError(this.Line, this.Column,
                        "An automatic variable cannot be declared from nil expression.");
                    this.IsOK = false;
                    return;
                }
            }

            this.VariableInfo = new VariableInfo(name, new TigerTypeHolder(returnType), false);
            scope.Add(VariableInfo);
            this.IsOK = true;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.RightExpressionNode.GenerateCode(moduleBuilder);
            this.VariableInfo.ParameterExpression =
                Expression.Parameter(this.VariableInfo.Holder.TigerType.GetCLRType());
        }
    }
}
