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

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace YATC.Scope
{
    /// <summary>
    /// Implementa la tabla de símbolos junto a TigerGlobal
    /// </summary>
    public class TigerScope
    {
        public TigerScope()
        {
        }

        private TigerScope(TigerScope parent, int parentIndex)
        {
            Parent = parent;
            ParentIndex = parentIndex;
        }

        private int _index;

        /// <summary>
        /// Ambito padre que contiene a este scope
        /// </summary>
        public readonly TigerScope Parent;

        /// <summary>
        /// Permite ver variables del padre de 0 a ParentIndex
        /// </summary>
        public readonly int ParentIndex;

        public bool IsRoot { get { return Parent == null; } }

        private readonly List<TigerScope> _children = new List<TigerScope>();
        private readonly HashSet<TigerTypeInfo> _typeInfos = new HashSet<TigerTypeInfo>();
        private readonly HashSet<FunVarInfo> _funVarInfos = new HashSet<FunVarInfo>();

        public TigerScope[] GetChildren()
        {
            return _children.ToArray();
        }

        public TigerScope CreateChildScope()
        {
            return new TigerScope(this, _index);
        }

        public bool Add(VariableInfo variableInfo)
        {
            Debug.WriteLine("Added to variable scope: {0} of type {1}", 
                variableInfo.Name, 
                variableInfo.Holder.TigerType != null ? variableInfo.Holder.TigerType.Name : "null");
            return _funVarInfos.Add(variableInfo) && ++_index != 0;
        }

        public bool Add(FunctionInfo functionInfo)
        {
            Debug.WriteLine("Added to function scope: {0} of type {1}", 
                functionInfo.Name, 
                functionInfo.Holder.TigerType.Name);
            return _funVarInfos.Add(functionInfo) && ++_index != 0;
        }

        public bool Add(TigerTypeInfo tigerTypeInfo)
        {
            Debug.WriteLine("Added to type scope: {0} of type {1}", 
                tigerTypeInfo.Name, 
                tigerTypeInfo.Holder.TigerType != null ? tigerTypeInfo.Holder.TigerType.Name: "null");
            return _typeInfos.Add(tigerTypeInfo) && ++_index != 0;
        }

        public bool ContainsType(TigerType tigerType, bool localSearchOnly)
        {
            bool isLocal = _typeInfos.Any(x => x.Holder.TigerType.Equals(tigerType));
            return isLocal || (!localSearchOnly && !IsRoot && Parent.ContainsType(tigerType, false));
        }

        public VariableInfo[] GetLocalVariableInfos()
        {
            return _funVarInfos.Where(x => x is VariableInfo).Cast<VariableInfo>().ToArray();
        }

        public VariableInfo FindVariableInfo(string name, bool localSearchOnly)
        {
            var result = _funVarInfos.FirstOrDefault(x => x.Name == name);
            if (result is FunctionInfo)
                return null;
            return (VariableInfo)result ?? (!localSearchOnly && !IsRoot ? Parent.FindVariableInfo(name, false) : null);
        }

        public bool CanFindVariableInfo(string name, bool localSearchOnly)
        {
            return FindVariableInfo(name, localSearchOnly) != null;
        }

        public TigerTypeInfo[] GetLocalTypeInfos()
        {
            return _typeInfos.ToArray();
        }

        public TigerTypeInfo FindTypeInfo(string name, bool localSearchOnly)
        {
            var result = _typeInfos.FirstOrDefault(x => x.Name == name);
            return result ?? (!localSearchOnly && !IsRoot ? Parent.FindTypeInfo(name, false) : null);
        }

        public bool CanFindTypeInfo(string name, bool localSearchOnly)
        {
            return FindTypeInfo(name, localSearchOnly) != null;
        }

        public FunctionInfo[] GetFunctionInfos()
        {
            return _funVarInfos.Where(x => x is FunctionInfo).Cast<FunctionInfo>().ToArray();
        }

        public FunctionInfo FindFunctionInfo(string name, bool localSearchOnly)
        {
            FunVarInfo result = _funVarInfos.FirstOrDefault(x => x.Name == name);
            if (result is VariableInfo)
                return null;
            return (FunctionInfo)result ?? (!localSearchOnly && !IsRoot ? Parent.FindFunctionInfo(name, false) : null);
        }

        public bool CanFindFunctionInfo(string name, bool localSearchOnly)
        {
            return FindFunctionInfo(name, localSearchOnly) != null;
        }

        public FunVarInfo FindFunVarInfo(string name, bool localSearchOnly)
        {
            FunVarInfo result = _funVarInfos.FirstOrDefault(x => x.Name == name);
            return result ?? (!localSearchOnly && !IsRoot ? Parent.FindFunctionInfo(name, false) : null);
        }

        public bool CanFindFunVarInfo(string name, bool localSearchOnly)
        {
            return FindFunVarInfo(name, localSearchOnly) != null;
        }

    }
}
