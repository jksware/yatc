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
    class RecordInstNode : ExpressionNode
    {
        public RecordInstNode(IToken payload)
            : base(payload)
        {
        }

        public TypeNode IdNode { get { return (TypeNode)TigerChildren[0]; } }
        public FieldInstNode[] FieldInstNodes
        {
            get
            {
                return TigerChildren.Length > 1 ?
                    TigerChildren[1].TigerChildren.Cast<FieldInstNode>().ToArray() :
                    new FieldInstNode[] {};
            }
        }
        private RecordType _recordType;

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            TigerTypeInfo recordInfo = scope.FindTypeInfo(this.IdNode.Name, false);
            if (recordInfo == null)
            {
                report.AddError(this.Line, this.Column,
                    "Undeclared record type: '{0}'.", this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            if (recordInfo.Holder.TigerType.Basetype != BaseType.Record)
            {
                report.AddError(this.Line, this.Column,
                    "Type mismatch: given type is not a record: '{0}'.", this.IdNode.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            _recordType = recordInfo.Holder.TigerType as RecordType;
            if (_recordType == null)
                throw new NullReferenceException();

            if (_recordType.FieldInfos.Length != this.FieldInstNodes.Length)
            {
                report.AddError(this.Line, this.Column,
                    "Record fields length mismatch '{0}': expecting {1} and {2} found.",
                    this.IdNode.Name,
                    _recordType.FieldInfos.Length,
                    this.FieldInstNodes.Length);
                this.TigerType = TigerType.Error;
                return;
            }

            for (int i = 0; i < this.FieldInstNodes.Length; i++)
            {
                FieldInstNode field = this.FieldInstNodes[i];

                if (field.IdNode.Name != _recordType.FieldInfos[i].Name)
                {
                    report.AddError(this.Line, this.Column,
                        "Field name mismatch: field number {0} of type '{1}' should be called '{2}' instead of '{3}'.",
                        i.ToString(),
                        this.IdNode.Name,
                        _recordType.FieldInfos[i].Name,
                        field.IdNode.Name);
                    this.TigerType = TigerType.Error;
                    return;
                }

                field.ExpressionNode.CheckSemantics(scope, report);
                if (!field.ExpressionNode.IsOk)
                {
                    this.TigerType = TigerType.Error;
                    return;
                }

                if (!field.ExpressionNode.TigerType.IsAssignableTo(_recordType.FieldInfos[i].Holder.TigerType))
                {
                    report.AddError(this.Line, this.Column,
                        "Type mismatch: field '{1}' of type '{0}' should be of type '{2}' instead of '{3}'",
                        this.IdNode.Name, field.IdNode.Name,
                        _recordType.FieldInfos[i].Holder.TigerType.Name, field.ExpressionNode.TigerType.Name);
                    this.TigerType = TigerType.Error;
                    return;
                }
            }

            this.TigerType = _recordType;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            NewExpression ctor = Expression.New(this.TigerType.GetCLRType());
            ParameterExpression tmp = Expression.Parameter(this.TigerType.GetCLRType());
            var fieldBindings = new MemberBinding[_recordType.FieldInfos.Length];

            for (int i = 0; i < FieldInstNodes.Length; i++)
            {
                this.FieldInstNodes[i].ExpressionNode.GenerateCode(moduleBuilder);

                fieldBindings[i] = Expression.Bind(
                    _recordType.GetCLRType().GetMember(_recordType.FieldInfos[i].Name)[0],
                    Expression.Convert(
                        this.FieldInstNodes[i].ExpressionNode.VmExpression,
                        _recordType.FieldInfos[i].Holder.TigerType.GetCLRType()
                    )
                );
            }

            Expression initializer = Expression.MemberInit(ctor, fieldBindings);

            var assign = Expression.Assign(tmp, initializer);
            BlockExpression initBlockExpression = Expression.Block(
                    new ParameterExpression[] { tmp },
                    new Expression[] { assign, tmp });

            this.VmExpression = initBlockExpression;
        }
    }
}
