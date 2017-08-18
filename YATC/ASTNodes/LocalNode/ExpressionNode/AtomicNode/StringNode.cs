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
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using YATC.Scope;

namespace YATC.ASTNodes
{
    class StringNode : AtomicNode
    {
        public StringNode(IToken payload)
            : base(payload)
        {
            string tmp = Text.Substring(1, Text.Length - 2);
            tmp = Regex.Replace(tmp, @"\\(\n|\r|\t|\s)+\\", string.Empty);
            tmp = Regex.Replace(tmp, @"(\\\d\d\d)", ToAscii);
            Value = Regex.Unescape(tmp);
        }

        public string Value { get; private set; }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            this.TigerType = TigerType.String;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            this.VmExpression = Expression.Constant(this.Value);
        }

        private string ToAscii(Match m)
        {
            var a = int.Parse(m.Groups[0].Value.Substring(1));
            return Convert.ToChar(a).ToString();
        }
    }
}
