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

namespace YATC.Scope
{
    public class VariableInfo : FunVarInfo
    {
        /// <summary>
        /// Determina si el atributo es un paramétro o no
        /// </summary>
        public bool IsParameter { get; set; }

        /// <summary>
        /// Whether the variable cannot be a left value of an assignment
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Refleja una variable dentro del ejecutable para la generación de código
        /// </summary>
        public ParameterExpression ParameterExpression { get; set; }

        public VariableInfo(string name, TigerTypeHolder holder, bool isParameter)
        {
            this.Name = name;
            this.Holder = holder;
            this.IsParameter = isParameter;
        }
        
    }
}
