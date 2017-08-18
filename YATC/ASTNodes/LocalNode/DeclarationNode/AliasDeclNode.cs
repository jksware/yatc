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
    class AliasDeclNode : DeclarationNode
    {
        public AliasDeclNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode TypeNode { get { return (TypeNode)TigerChildren[0]; } }

        public override bool CheckHeader(TigerScope scope, Report report, string name)
        {
            this.TigerTypeInfo = new TigerTypeInfo(name, new TigerTypeHolder(), false);
            scope.Add(this.TigerTypeInfo);
            return true;
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            // type is an alias of another type, then follow.
            TigerTypeInfo aliasTo = scope.FindTypeInfo(this.TypeNode.Name, false);

            if (aliasTo == null)
            {
                report.AddError(this.Line, this.Column, "Alias to undeclared type: '{0}'.", this.TypeNode.Name);
                this.IsOK = false;
                return;
            }

            this.TigerTypeInfo.Holder.TigerType = aliasTo.Holder.TigerType;
            this.IsOK = true;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            // do nothing
        }
    }
}
