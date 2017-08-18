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
using System.Reflection;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    internal class RecordDeclNode : DeclarationNode
    {
        public RecordDeclNode(IToken payload)
            : base(payload)
        {
        }

        public TypeFieldNode[] TypeFieldNodes
        {
            get
            {
                return TigerChildren.Length > 0 ? TigerChildren.Cast<TypeFieldNode>().ToArray() : new TypeFieldNode[] {};
            }
        }

        public TypeBuilder TypeBuilder { get; private set; }

        public override bool CheckHeader(TigerScope scope, Report report, string name)
        {
            this.TigerTypeInfo = new TigerTypeInfo(name, new TigerTypeHolder(), false);
            scope.Add(this.TigerTypeInfo);
            return true;
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            var hash = new HashSet<string>();
            foreach (var typeFieldNode in this.TypeFieldNodes)
            {
                typeFieldNode.TypeNode.CheckSemantics(scope, report);

                if (!typeFieldNode.TypeNode.IsOk)
                {
                    this.TigerTypeInfo.Holder.TigerType = TigerType.Error;
                    this.IsOK = false;
                    return;
                }

                string name = typeFieldNode.IdNode.Name;
                if (!hash.Add(name))
                {
                    report.AddError(this.Line, this.Column, "Redeclared field name: '{0}'.", name);
                    this.TigerTypeInfo.Holder.TigerType = TigerType.Error;
                    this.IsOK = false;
                    return;
                }
            }

            VariableInfo[] fieldInfos = new VariableInfo[this.TypeFieldNodes.Length];

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                TigerTypeInfo fieldTigerTypeInfo = scope.FindTypeInfo(this.TypeFieldNodes[i].TypeNode.Name, false);
                if (fieldTigerTypeInfo == null)
                {
                    report.AddError(this.Line, this.Column, "Undeclared field type: '{0}'.",
                        this.TypeFieldNodes[i].TypeNode.Name);
                    this.TigerTypeInfo.Holder.TigerType = TigerType.Error;
                    this.IsOK = false;
                    return;
                }

                fieldInfos[i] = new VariableInfo(this.TypeFieldNodes[i].Name, fieldTigerTypeInfo.Holder, false);
            }

            this.TigerTypeInfo.Holder.TigerType = new RecordType(this.TigerTypeInfo.Name, fieldInfos);
            this.IsOK = true;
        }

        public void GenerateHeaderCode(ModuleBuilder moduleBuilder)
        {
            string name = string.Format("{0}_{1}", this.TigerTypeInfo.Name, ProgramNode.RecordNumber++);
            this.TigerTypeInfo.Holder.TigerType.CLRType =
                this.TypeBuilder = moduleBuilder.DefineType(name, TypeAttributes.Public);
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            foreach (var variableInfo in ((RecordType)this.TigerTypeInfo.Holder.TigerType).FieldInfos)
            {
                this.TypeBuilder.DefineField(
                    variableInfo.Name,
                    variableInfo.Holder.TigerType.GetCLRType(),
                    FieldAttributes.Public);
            }

            // Finish the type.
            this.TigerTypeInfo.Holder.TigerType.CLRType = this.TypeBuilder.CreateType();
        }
    }
}
