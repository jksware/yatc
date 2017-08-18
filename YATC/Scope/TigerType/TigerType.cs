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

using System;
using System.Diagnostics;

namespace YATC.Scope
{
    public enum BaseType
    {
        Error = -1,
        Int = 0,
        String = 1,
        Nil = 2,
        Record = 3,
        Array = 4,
        Void = 5,
        FillIn = 6,
        Unknown = 7
    }

    public class TigerType
    {
        public string Name { get; protected set; }
        public BaseType Basetype { get; private set; }

        public Type CLRType { get; set; }

        protected TigerType(string name, BaseType basetype)
        {
            this.Name = name;
            this.Basetype = basetype;
        }

        protected TigerType(BaseType baseType)
            : this(baseType.ToString().ToLowerInvariant(), baseType)
        {
        }

        public virtual Type GetCLRType()
        {
            if (CLRType != null)
                return CLRType;

            switch (Basetype)
            {
                case BaseType.Int:
                    return typeof(int);
                case BaseType.String:
                    return typeof(string);
                case BaseType.Void:
                    return typeof(void);
                case BaseType.Nil:
                    return null;
                case BaseType.Array:
                    return (this as ArrayType).ElementTypeHolder.TigerType.GetCLRType().MakeArrayType();
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static readonly TigerType Error = new TigerType(BaseType.Error);
        public static readonly TigerType Int = new TigerType(BaseType.Int);
        public static readonly TigerType String = new TigerType(BaseType.String);
        public static readonly TigerType Nil = new TigerType(BaseType.Nil);
        public static readonly TigerType Void = new TigerType(BaseType.Void);
        public static readonly TigerType FillIn = new TigerType(BaseType.FillIn);
        public static readonly TigerType Unknown = new TigerType(BaseType.Unknown);

        public virtual bool IsAssignableTo(TigerType other)
        {
            bool result = this.Equals(other) ||
                          (this.Basetype != BaseType.Record &&
                           this.Basetype != BaseType.Array && this.Basetype == other.Basetype) ||
                          (this.Basetype == BaseType.Nil && other.Basetype == BaseType.Array) ||
                          (this.Basetype == BaseType.Nil && other.Basetype == BaseType.Record) ||
                          (this.Basetype == BaseType.Nil && other.Basetype == BaseType.String);
            Debug.WriteLine("Typesystem ruled that '{0}' {2} assignable to '{1}'.",
                this.Name, other.Name, result ? "is" : "is NOT");
            return result;
        }
    }
}
