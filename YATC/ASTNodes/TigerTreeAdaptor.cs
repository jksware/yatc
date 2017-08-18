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
using Antlr.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using YATC.Grammar;

namespace YATC.ASTNodes
{
    public class TigerTreeAdaptor : CommonTreeAdaptor
    {
        private readonly Dictionary<int, Type> _payloadCache;

        public TigerTreeAdaptor()
        {
            _payloadCache = new Dictionary<int, Type>
                {
                    {tigerParser.INTKEY, typeof (TypeNode)},
                    {tigerParser.STRINGKEY, typeof (TypeNode)},
                    {tigerParser.NILKEY, typeof (TypeNode)}
                };

            FieldInfo[] _fields = typeof(tigerParser).GetFields();
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            foreach (var field in _fields)
            {
                if (!field.IsStatic)
                    continue;

                string name = GetName(field.Name);
                Type type = executingAssembly.GetType(name);                
                if (type != null)
                    _payloadCache[(int)field.GetRawConstantValue()] = type;
            }
        }

        private string GetName(string name)
        {
            var sb = new StringBuilder();
            foreach (var x in name.Split('_'))
                sb.Append(char.ToUpper(x[0]) + x.Substring(1, x.Length - 1).ToLower());
            return string.Format("YATC.ASTNodes.{0}Node", sb);
        }

        public override object Create(IToken payload)
        {
            if (payload == null)
                return new UnknownNode(null);
                //return base.Create(null);

            Type type;
            bool foundType = _payloadCache.TryGetValue(payload.Type, out type);
            return foundType ? Activator.CreateInstance(type, payload) : new UnknownNode(payload);
        }
    }
}
