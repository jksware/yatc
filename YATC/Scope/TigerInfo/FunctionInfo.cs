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

using System.Linq.Expressions;
using System.Reflection;

namespace YATC.Scope
{
    public class FunctionInfo : FunVarInfo
    {
        public FunctionInfo(string name, VariableInfo[] parameterInfo, TigerTypeHolder returnTypeHolder, bool isStandard)
        {
            this.Name = name;
            this.ParameterInfo = parameterInfo;
            this.Holder = returnTypeHolder;
            this.IsStandard = isStandard;
        }

        public FunctionInfo(string name, VariableInfo[] parameterInfo, TigerTypeHolder returnTypeHolder, 
            MethodInfo methodInfo)
            :this(name, parameterInfo, returnTypeHolder, true)
        {
            this.MethodInfo = methodInfo;
        }

        public MethodInfo MethodInfo;

        /// <summary>
        /// Listado de parámetros
        /// </summary>
        public readonly VariableInfo[] ParameterInfo;

        /// <summary>
        /// Función en la máquina virtual correspondiente
        /// </summary>
        public Expression LambdaExpression;
    }
}
