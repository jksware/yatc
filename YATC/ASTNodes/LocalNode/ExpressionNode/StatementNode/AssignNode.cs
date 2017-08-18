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
    class AssignNode : StatementNode
    {
        public AssignNode(IToken payload)
            : base(payload)
        {
        }

        public VarAccessNode LeftValueNode { get { return (VarAccessNode)TigerChildren[0]; } }
        public ExpressionNode RightExpressionNode { get { return (ExpressionNode)TigerChildren[1]; } }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.LeftValueNode.CheckSemantics(scope, report);
            this.RightExpressionNode.CheckSemantics(scope, report);

            // No se reporta el error para evitar cascadas de errores
            this.TigerType = this.LeftValueNode.IsOk && this.RightExpressionNode.IsOk
                ? TigerType.Void
                : TigerType.Error;

            if (!this.IsOk)
                return;

            if (this.RightExpressionNode.TigerType.Equals(TigerType.Void))
            {
                report.AddError(this.Line, this.Column, "Right hand side must evaluate to a returning value.");
                this.TigerType = TigerType.Error;
                return;
            }

            /* Assignment */
            if (!this.RightExpressionNode.TigerType.IsAssignableTo(this.LeftValueNode.TigerType))
            {
                report.AddError(this.Line, this.Column,
                                "Type mismatch: Types of variable declaration and expression do not match: '{0}' and '{1}'",
                                this.LeftValueNode.TigerType.Name, this.RightExpressionNode.TigerType.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            // checks if left variable is not read-only, i.e., it is not defined in a ForNode
            // la segunda pregunta accessNode == null es por si se quiere agregar campos o arrays
            if (this.LeftValueNode.VariableInfo.IsReadOnly && this.LeftValueNode.AccessNode == null)
            {
                this.TigerType = TigerType.Error;
                report.AddError(this.Line, this.Column,
                    "Cannot assign to a read-only variable (it may be declared within a for control structure).");
                return;
            }

            this.TigerType = TigerType.Void;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.LeftValueNode.GenerateCode(moduleBuilder);
            this.RightExpressionNode.GenerateCode(moduleBuilder);

            //int isOK = (int)(34 < 342);

            this.VmExpression =
                Expression.Block(
                    Expression.Assign(
                        this.LeftValueNode.VmExpression,
                        Expression.Convert(
                            this.RightExpressionNode.VmExpression,
                            this.LeftValueNode.TigerType.GetCLRType()
                            )
                        ),
                    Expression.Empty());
        }
    }
}
