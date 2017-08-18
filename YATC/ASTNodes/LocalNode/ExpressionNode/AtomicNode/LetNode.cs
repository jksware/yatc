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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class LetNode : AtomicNode
    {
        public LetNode(IToken payload)
            : base(payload)
        {
        }

        public IEnumerable<DeclarationNode> DeclarationNodes { get { return TigerChildren[0].TigerChildren.Cast<DeclarationNode>(); } }
        public ExprSeqNode ExprSeqNode
        {
            get
            {
                return (ExprSeqNode)TigerChildren[1];
            }
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            TigerScope innerScope = scope.CreateChildScope();

            foreach (var declarationNode in this.DeclarationNodes)
            {
                declarationNode.CheckSemantics(innerScope, report);
                if (!declarationNode.IsOK)
                {
                    this.TigerType = TigerType.Error;
                    return;
                }
            }

            this.ExprSeqNode.CheckSemantics(innerScope, report);
            if (!this.ExprSeqNode.IsOk || scope.ContainsType(this.ExprSeqNode.TigerType, false))
                this.TigerType = this.ExprSeqNode.TigerType;
            else
            {
                report.AddError(this.Line, this.Column,
                    "Type mismatch: Type '{0}' returned from let declaration is not " +
                    "defined in an outer scope, or it is a different definition.",
                    this.ExprSeqNode.TigerType.Name);
                this.TigerType = TigerType.Error;
            }
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            foreach (var declarationNode in DeclarationNodes)
                declarationNode.GenerateCode(moduleBuilder);

            if (this.ExprSeqNode.ExpressionNodes != null)
                foreach (var expressionNode in this.ExprSeqNode.ExpressionNodes)
                    expressionNode.GenerateCode(moduleBuilder);

            // variables
            IEnumerable<VarDeclNode> varDeclNodes =
                this.DeclarationNodes.Where(x => x is VarDeclNode).Cast<VarDeclNode>();

            IEnumerable<ParameterExpression> variables =
                varDeclNodes.Select(x => x.VariableInfo.ParameterExpression);

            IEnumerable<Expression> initVariablesExpressions =
                varDeclNodes.Select(varDeclNode =>
                    Expression.Assign(
                        varDeclNode.VariableInfo.ParameterExpression,
                        Expression.Convert(
                            varDeclNode.RightExpressionNode.VmExpression,
                            varDeclNode.VariableInfo.Holder.TigerType.GetCLRType()
                        )
                    )
                );

            // fundeclseq
            IEnumerable<FunDeclSeqNode> funDeclSeqNodes =
                this.DeclarationNodes.Where(x => x is FunDeclSeqNode).Cast<FunDeclSeqNode>();

            // final
            IEnumerable<Expression> blockExpressions = initVariablesExpressions.Concat(
                this.ExprSeqNode.ExpressionNodes != null ? 
                    (this.ExprSeqNode.ExpressionNodes.Select(x => x.VmExpression)) : 
                    new Expression[] { Expression.Empty() }
                );

            var functionClousures = new List<ParameterExpression>();
            var functionAssigns = new List<Expression>();
            foreach (var funDeclSeqNode in funDeclSeqNodes)
            {
                functionClousures.AddRange(funDeclSeqNode.FunctionClousures);
                functionAssigns.AddRange(funDeclSeqNode.FunctionAssigns);
            }

            this.VmExpression = Expression.Block(
                functionClousures.Concat(variables),
                functionAssigns.Concat(blockExpressions));

        }
    }
}
