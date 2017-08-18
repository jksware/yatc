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
    class TypeDeclNode : DeclarationNode
    {
        public TypeDeclNode(IToken payload)
            : base(payload)
        {
        }

        public string Name { get { return this.IdNode.Name; } }

        public IdNode IdNode { get { return (IdNode)TigerChildren[0]; } }
        public DeclarationNode DeclarationNode { get { return (DeclarationNode)TigerChildren[1]; } }

        public bool IsAliasNode { get { return this.DeclarationNode is AliasDeclNode; } }
        public bool IsRecordNode { get { return this.DeclarationNode is RecordDeclNode; } }
        public bool IsArrayNode { get { return this.DeclarationNode is ArrayDeclNode; } }

        public override bool CheckHeader(TigerScope scope, Report report, string name)
        {
            if (scope.CanFindTypeInfo(this.Name, true))
            {
                report.AddError(this.Line, this.Column, "Redeclared local type: '{0}'.", this.Name);
                this.IsOK = false;
                return false;
            }

            TigerTypeInfo outerTypeInfo = scope.FindTypeInfo(this.Name, false);
            if (outerTypeInfo != null)
                if (outerTypeInfo.IsStandard)
                {
                    report.AddError(this.Line, this.Column,
                        "Redeclared standard type: '{0}'.",
                        this.Name);
                    this.IsOK = false;
                    return false;
                }
                else
                    report.AddWarning(this.Line, this.Column, "Type name hides outer scope type: '{0}'.", this.Name);

            return this.DeclarationNode.CheckHeader(scope, report, this.IdNode.Name);
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.DeclarationNode.CheckSemantics(scope, report);
            this.IsOK = this.DeclarationNode.IsOK;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.DeclarationNode.GenerateCode(moduleBuilder);
        }
    }
}
