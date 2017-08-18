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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class FieldAccessNode : AccessNode
    {
        private TigerType _parentType;

        public FieldAccessNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }
        public AccessNode AccessNode { get { return TigerChildren.Length > 1 ? (AccessNode)TigerChildren[1] : null; } }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            if (this.ParentType.Basetype != BaseType.Record)
            {
                report.AddError(this.Line, this.Column,
                    "Type mismatch: Variable or field is not a record: '{0}' was found.",
                    this.ParentType.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            VariableInfo[] fieldInfos = ((RecordType)this.ParentType).FieldInfos;
            if (fieldInfos.Length == 0)
            {
                report.AddError(this.Line, this.Column,
                    "Cannot access field on empty record type '{0}': '{1}'",
                    this.ParentType.Name, this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            var fieldInfo = fieldInfos.FirstOrDefault(x => x.Name == this.IdNode.Name);

            if (fieldInfo == null)
            {
                report.AddError(this.Line, this.Column, 
                    "Record type '{0}' does not contain a definition for '{1}'.",
                    this.ParentType.Name, this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            _parentType = fieldInfo.Holder.TigerType;

            if (this.AccessNode == null)
            {
                this.TigerType = _parentType;
            }
            else
            {
                this.AccessNode.ParentType = _parentType;
                this.AccessNode.CheckSemantics(scope, report);
                this.TigerType = AccessNode.TigerType;
            }
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.VmExpression = Expression.MakeMemberAccess(
                this.VmExpression, 
                this.ParentType.GetCLRType().GetMember(this.IdNode.Name)[0]
            );

            if (this.AccessNode == null)
                return;

            this.AccessNode.VmExpression = this.VmExpression;
            this.AccessNode.GenerateCode(moduleBuilder);
            this.VmExpression = this.AccessNode.VmExpression;
        }
    }
}
