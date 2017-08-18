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
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using YATC.Scope;

namespace YATC.ASTNodes
{
    public class ProgramNode : TigerNode
    {
        public ProgramNode(IToken payload)
            : base(payload)
        {
            Report = new Report();
            Scope = new TigerScope();
            RecordNumber = 0;
            AddStdLib();
        }

        public readonly Report Report;
        internal readonly TigerScope Scope;
        internal static int RecordNumber;

        private FunctionInfo _print;
        private FunctionInfo _printLine;
        private FunctionInfo _printi;
        private FunctionInfo _printiline;
        private FunctionInfo _getline;
        private FunctionInfo _ord;
        private FunctionInfo _chr;
        private FunctionInfo _exit;
        private FunctionInfo _not;
        private FunctionInfo _concat;
        private FunctionInfo _substring;
        private FunctionInfo _size;

        internal ExpressionNode ExpressionNode { get { return (ExpressionNode)GetChild(0); } }

        public void CheckSemantics()
        {
            if (this.ExpressionNode != null)
                this.ExpressionNode.CheckSemantics(Scope, Report);
        }

        public AssemblyBuilder GenerateCode(string name, string fileName, string outputDir)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName,
                                                                                     AssemblyBuilderAccess.RunAndSave,
                                                                                     outputDir);
            ModuleBuilder moduleBuilder = assembly.DefineDynamicModule(fileName, fileName);
            TypeBuilder typeBuilder = moduleBuilder.DefineType("yatcProgram");
            MethodBuilder mainMethod = typeBuilder.DefineMethod("main", MethodAttributes.Static);

            Expression mainBlock;
            if (this.ExpressionNode != null)
            {
                this.ExpressionNode.GenerateCode(moduleBuilder);

                ParameterExpression outcode = Expression.Parameter(typeof(int));
                ParameterExpression exception = Expression.Parameter(typeof(Exception));

                MemberInfo excMessageMember = typeof(Exception).GetMember("Message")[0];
                MemberInfo consoleError = typeof(Console).GetMember("Error")[0];
                MethodInfo errorWrite = typeof(TextWriter).GetMethod("WriteLine", new Type[] { typeof(string) });

                mainBlock = Expression.Block(
                    new ParameterExpression[] { outcode, exception },
                    new Expression[]
                        {
                            Expression.Assign(outcode, Expression.Constant(0)),
                            Expression.TryCatch(
                                Expression.Block(
                                    this.ExpressionNode.VmExpression,
                                    Expression.Empty()
                                ),
                                new CatchBlock[]
                                    {
                                        Expression.MakeCatchBlock(
                                            typeof(Exception), 
                                            exception,
                                            Expression.Block(
                                                Expression.Call(
                                                    Expression.MakeMemberAccess(null, consoleError),
                                                    errorWrite, 
                                                    Expression.MakeMemberAccess(exception, excMessageMember)
                                                ),
                                                Expression.Assign(outcode, Expression.Constant(1)),
                                                Expression.Empty()
                                            ),
                                            Expression.Constant(true)
                                        )
                                    }
                            ),
                            outcode
                        });
            }
            else
            {
                mainBlock = Expression.Constant(0);
            }

            LambdaExpression lambdaMainBlock = Expression.Lambda<Func<int>>(mainBlock);
            lambdaMainBlock.CompileToMethod(mainMethod);
            assembly.SetEntryPoint(mainMethod);

            typeBuilder.CreateType();
            moduleBuilder.CreateGlobalFunctions();
            return assembly;
        }

        private void AddStdLib()
        {
            var stringHolder = new TigerTypeHolder(TigerType.String);
            var intHolder = new TigerTypeHolder(TigerType.Int);
            var voidHolder = new TigerTypeHolder(TigerType.Void);

            _print = new FunctionInfo("print", new[] { new VariableInfo("s", stringHolder, true), }, voidHolder, true);
            _printLine = new FunctionInfo("printline", new[] { new VariableInfo("s", stringHolder, true), }, voidHolder, true);
            _printi = new FunctionInfo("printi", new[] { new VariableInfo("i", intHolder, true), }, voidHolder, true);
            _printiline = new FunctionInfo("printiline", new[] { new VariableInfo("i", intHolder, true), }, voidHolder, true);
            _getline = new FunctionInfo("getline", new VariableInfo[0], stringHolder, true);
            _ord = new FunctionInfo("ord", new[] { new VariableInfo("s", stringHolder, true), }, intHolder, true);
            _chr = new FunctionInfo("chr", new[] { new VariableInfo("i", intHolder, true), }, stringHolder, true);
            _size = new FunctionInfo("size", new[] { new VariableInfo("s", stringHolder, true), }, intHolder, true);
            _substring = new FunctionInfo("substring", new[]
                {
                    new VariableInfo("s", stringHolder, true), 
                    new VariableInfo("f", intHolder, true),
                    new VariableInfo("n", intHolder, true),
                },
                stringHolder, true);
            _concat = new FunctionInfo("concat",
                new[]
                {
                    new VariableInfo("s1", stringHolder, true),
                    new VariableInfo("s2", stringHolder, true),
                },
                stringHolder,
                true);
            _not = new FunctionInfo("not", new[] { new VariableInfo("i", intHolder, true), }, intHolder, true);
            _exit = new FunctionInfo("exit", new[] { new VariableInfo("i", intHolder, true), }, voidHolder, true);

            Scope.Add(_print);
            Scope.Add(_printLine);
            Scope.Add(_printi);
            Scope.Add(_printiline);
            Scope.Add(_getline);
            Scope.Add(_ord);
            Scope.Add(_chr);
            Scope.Add(_size);
            Scope.Add(_substring);
            Scope.Add(_concat);
            Scope.Add(_not);
            Scope.Add(_exit);

            Scope.Add(new TigerTypeInfo("string", new TigerTypeHolder(TigerType.String), true));
            Scope.Add(new TigerTypeInfo("int", new TigerTypeHolder(TigerType.Int), true));
            Scope.Add(new TigerTypeInfo("nil", new TigerTypeHolder(TigerType.Nil), true));

            // althought it is defined and can be returned as a valid program expression it cannot be called by name
            Scope.Add(new TigerTypeInfo("!void", new TigerTypeHolder(TigerType.Void), true));

            MethodInfo writeString = ((Action<string>)Console.Write).Method;
            MethodInfo writeLineString = ((Action<string>)Console.WriteLine).Method;
            MethodInfo writeInt = ((Action<int>)Console.Write).Method;
            MethodInfo writeLineInt = ((Action<int>)Console.WriteLine).Method;
            MethodInfo readLine = ((Func<string>)Console.ReadLine).Method;
            MethodInfo chr = ((Func<byte, string>)Convert.ToString).Method;
            MethodInfo concat = ((Func<string, string, string>)string.Concat).Method;

            _print.MethodInfo = writeString;
            _printLine.MethodInfo = writeLineString;
            _printi.MethodInfo = writeInt;
            _printiline.MethodInfo = writeLineInt;
            _getline.MethodInfo = readLine;
            //_chr.MethodInfo = chr;
            _concat.MethodInfo = concat;

            Expression<Func<int, string>> lambdaChr = (b) => ( new string(Convert.ToChar(((byte)(b) > 127) ? int.MaxValue : (byte)b), 1));
            Expression<Func<string, int>> lambdaOrd = (x) => (string.IsNullOrEmpty(x) ? -1 : Convert.ToByte(x[0]));
            Expression<Func<string, int>> lambdaSize = (x) => (x.Length);
            Expression<Func<string, int, int, string>> lambdaSubstring = (x, y, z) => (x.Substring(y, z));
            Expression<Func<int, int>> lambdaNot = (x) => (x == 0 ? 1 : 0);

            _ord.LambdaExpression = lambdaOrd;
            _size.LambdaExpression = lambdaSize;
            _substring.LambdaExpression = lambdaSubstring;
            _not.LambdaExpression = lambdaNot;
            _chr.LambdaExpression = lambdaChr;
        }

    }
}
