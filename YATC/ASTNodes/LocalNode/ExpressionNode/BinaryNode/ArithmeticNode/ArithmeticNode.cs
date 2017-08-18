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
    abstract class ArithmeticNode : BinaryNode
    {
        protected ArithmeticNode(IToken payload)
            : base(payload)
        {
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            base.CheckSemantics(scope, report);
            bool bothOk = this.LeftOperandNode.IsOk && this.RightOperandNode.IsOk;

            if (this.LeftOperandNode.IsOk)
            {
                if (this.LeftOperandNode.TigerType.Basetype == BaseType.Void)
                {
                    report.AddError(this.Line, this.Column,
                                    "Type mismatch: Invalid use of binary arithmetic operator with a non-valued left expression.");
                    this.TigerType = TigerType.Error;
                    bothOk = false;
                }
                else
                    if (this.LeftOperandNode.TigerType.Basetype != BaseType.Int)
                    {
                        report.AddError(this.Line, this.Column,
                                        "Type mismatch: Invalid use of binary arithmetic operator with a non-int left expression: '{0}' was found.",
                                        this.LeftOperandNode.TigerType.Name);
                        this.TigerType = TigerType.Error;
                        bothOk = false;
                    }
            }

            if (this.RightOperandNode.IsOk)
            {
                if (this.RightOperandNode.TigerType.Basetype == BaseType.Void)
                {
                    report.AddError(this.Line, this.Column,
                                    "Type mismatch: Invalid use of binary arithmetic operator with a non-valued right expression.");
                    this.TigerType = TigerType.Error;
                    bothOk = false;
                }
                else
                    if (this.RightOperandNode.TigerType.Basetype != BaseType.Int)
                    {
                        report.AddError(this.Line, this.Column,
                                        "Type mismatch: Invalid use of binary arithmetic operator with a non-int right expression: '{0}' was found.",
                                        this.RightOperandNode.TigerType.Name);
                        this.TigerType = TigerType.Error;
                        bothOk = false;
                    }
            }
            this.TigerType = bothOk ? TigerType.Int : TigerType.Error;
        }
    }
}
