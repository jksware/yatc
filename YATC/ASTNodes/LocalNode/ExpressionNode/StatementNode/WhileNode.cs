﻿/*
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
    class WhileNode : StatementNode, IBreakeableNode
    {
        public WhileNode(IToken payload)
            : base(payload)
        {
        }

        public LabelTarget BreakTarget { get; set; }

        public ExpressionNode ConditionExpression { get { return (ExpressionNode)TigerChildren[0]; } }

        public ExpressionNode DoExpression { get { return (ExpressionNode)TigerChildren[1]; } }


        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.ConditionExpression.CheckSemantics(scope, report);
            this.DoExpression.CheckSemantics(scope, report);

            if (!this.ConditionExpression.IsOk || !this.DoExpression.IsOk)
            {
                this.TigerType = TigerType.Error;
                return;
            }

            if (this.ConditionExpression.TigerType.Basetype != BaseType.Int)
            {
                report.AddError(Line, Column,
                    "Type mismatch: Expecting integer (or alias) type on condition expression: '{0}' was found.",
                    this.ConditionExpression.TigerType.Name);
                this.TigerType = TigerType.Error;
                return;
            }

            if (this.DoExpression.TigerType.Basetype != BaseType.Void)
            {
                this.TigerType = TigerType.Error;
                report.AddError(this.Line, this.Column, 
                    "Type mismatch: Expecting void return type in while expression: '{0}' was found.",
                    this.DoExpression.TigerType.Name);
                return;
            }

            this.TigerType = TigerType.Void;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            BreakTarget = Expression.Label();

            this.ConditionExpression.GenerateCode(moduleBuilder);
            this.DoExpression.GenerateCode(moduleBuilder);

            ConditionalExpression conditionalExpression = Expression.IfThenElse(
                Expression.NotEqual(this.ConditionExpression.VmExpression, Expression.Constant(0)),
                this.DoExpression.VmExpression,
                Expression.Break(this.BreakTarget));

            this.VmExpression = Expression.Loop(conditionalExpression, this.BreakTarget);
        }
    }
}
