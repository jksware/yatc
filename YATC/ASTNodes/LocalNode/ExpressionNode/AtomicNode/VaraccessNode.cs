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
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    internal class VarAccessNode : AtomicNode
    {
        public VarAccessNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }
        public AccessNode AccessNode { get { return TigerChildren.Length > 1 ? (AccessNode)TigerChildren[1] : null; } }

        public string Name { get { return IdNode.Text; } }
        public VariableInfo VariableInfo { get; set; }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.VariableInfo = scope.FindVariableInfo(this.Name, false);
            if (this.VariableInfo == null)
            {
                report.AddError(this.Line, this.Column, "Undeclared variable: '{0}'.", this.Name);
                this.TigerType = TigerType.Error;
                return;            
            }

            if (this.AccessNode != null)
            {
                this.AccessNode.ParentType = this.VariableInfo.Holder.TigerType;
                this.AccessNode.CheckSemantics(scope, report);
                this.TigerType = this.AccessNode.TigerType;
                return;
            }

            this.TigerType = VariableInfo.Holder.TigerType;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.VmExpression = this.VariableInfo.ParameterExpression;

            if (this.AccessNode == null)
                return;

            this.AccessNode.VmExpression = this.VmExpression;
            this.AccessNode.GenerateCode(moduleBuilder);
            this.VmExpression = this.AccessNode.VmExpression;
        }
    }
}
