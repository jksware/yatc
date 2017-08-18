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
using YATC.Scope;

namespace YATC.ASTNodes
{
    abstract class IdentityNode : RelationalNode
    {
        protected IdentityNode(IToken payload)
            : base(payload)
        {
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            base.CheckSemantics(scope, report);
            bool bothOk = this.LeftOperandNode.IsOk && this.RightOperandNode.IsOk;

            if (LeftOperandNode.IsOk && this.LeftOperandNode.TigerType.Basetype == BaseType.Void)
            {
                report.AddError(this.Line, this.Column,
                                "Type mismatch: Invalid use of binary identity operator with a non-valued left expression.");
                this.TigerType = TigerType.Error;
                bothOk = false;
            }

            if (this.RightOperandNode.IsOk && this.RightOperandNode.TigerType.Basetype == BaseType.Void)
            {
                report.AddError(this.Line, this.Column,
                                "Type mismatch: Invalid use of binary identity operator with a non-valued right expression.");
                this.TigerType = TigerType.Error;
                bothOk = false;
            }

            if (this.LeftOperandNode.TigerType.Basetype == BaseType.Nil &&
                this.RightOperandNode.TigerType.Basetype == BaseType.Nil)
            {
                report.AddError(this.Line, this.Column,
                                "Type mismatch: Invalid use of binary identity operator with two nils.");
                this.TigerType = TigerType.Error;
                bothOk = false;
            }

            if (bothOk &&
                !LeftOperandNode.TigerType.IsAssignableTo(RightOperandNode.TigerType) &&
                !RightOperandNode.TigerType.IsAssignableTo(LeftOperandNode.TigerType))
            {
                report.AddError(this.Line, this.Column,
                    "Type mismatch: Invalid use of binary identity operator with different types: '{0}' and '{1}' were found.",
                    this.LeftOperandNode.TigerType.Name,
                    this.RightOperandNode.TigerType.Name);
                this.TigerType = TigerType.Error;
                bothOk = false;
            }

            this.TigerType = bothOk ? TigerType.Int : TigerType.Error;
        }
    }
}
