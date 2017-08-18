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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using YATC.ASTNodes;
using YATC.Grammar;

namespace YATC.Compiler
{
    static class Program
    {
        enum ErrorKind { Fatal, Syntax, Semantic }

        public static readonly string FileName = Assembly.GetExecutingAssembly().GetName().Name;
        public const string Name = "Yet Another Tiger Compiler";
        public const string Copyright = "Copyright D. Valdes Santiago & Juan C. Pujol Mainegra (c) 2013-2014";
        public const string ShortName = "YATC";

        private static TextWriterTraceListener _textListener = null;
        private const string MessageFormatHead = "({0}, {1}): ";
        private const string MessageFormatBody = "{0} {1}: {2}";

        private const string Usage =
            "Usage :" +
            "\ttiger.exe path_to_program_file.tig"
                             ;

        static void Write(int line, int column, ErrorKind errorKind, Level level, string message, params object[] objects)
        {
            string msg = string.Format(message, objects);
            string wrappedMsg = string.Format(MessageFormatHead, line, column);
            Trace.Write(wrappedMsg);

            ConsoleColor lastColor = Console.ForegroundColor;
            switch (level)
            {
                case Level.Info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case Level.Warning:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case Level.Error:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }

            wrappedMsg = string.Format(MessageFormatBody, errorKind, level, msg);
            Trace.WriteLine(wrappedMsg);

            Console.ForegroundColor = lastColor;
        }

        static void PrintUsage()
        {
            Trace.WriteLine(Usage);
        }

        static void PrintWelcome()
        {
            Trace.WriteLine(Name + " version " + Assembly.GetExecutingAssembly().GetName().Version + '\n' + Copyright);
        }

        static void PrintBlank()
        {
            Trace.WriteLine(string.Empty);
        }

        static void Flush()
        {
            Trace.Flush();
            Debug.Flush();
            if (_textListener != null)
            {
                _textListener.Flush();
                _textListener.Close();
            }
        }

        static int ExitBad(string programName)
        {
            Flush();
            Write(0, 0, ErrorKind.Fatal, Level.Error,
                "Could not generate executable for program '{0}': One or more errors were found.",
                programName);
            PrintBlank();
            return 1;
        }

        static int Main(string[] args)
        {
            try
            {
                _textListener = new TextWriterTraceListener(FileName + ".log");
                Trace.Listeners.Add(_textListener);

                _textListener.WriteLine(string.Empty.PadLeft(79, '='));
                _textListener.WriteLine(string.Format("Tracing log for program '{0}' on '{1}'.",
                    args.Length >= 1 ? args[0] : "not specified", DateTime.Now.ToString(CultureInfo.InvariantCulture)));
#if DEBUG
                _textListener.WriteLine("Debug mode is ON.");
#else
                _textListener.WriteLine("Debug mode is OFF.");
#endif
                _textListener.WriteLine(string.Empty.PadLeft(79, '='));
            }
            catch
            {
            }
            Trace.Listeners.Add(new ConsoleTraceListener());

            PrintWelcome();
            PrintBlank();

            if (args.Length != 1)
            {
                Write(0, 0, ErrorKind.Fatal, Level.Error,
                    "Wrong number of argument: expecting one and {0} found.",
                    args.Length);
                return ExitBad(string.Empty);
            }

            ProgramNode programNode;
            try
            {
                ICharStream charStream = new ANTLRFileStream(args[0]);
                var lexer = new tigerLexer(charStream);
                var tokenStream = new CommonTokenStream(lexer);
                var parser = new tigerParser(tokenStream)
                                 {
                                     TraceDestination = Console.Out,
                                     TreeAdaptor = new TigerTreeAdaptor()
                                 };

                programNode = parser.a_program().Tree as ProgramNode;
            }
            catch (ParsingException e)
            {
                Write(e.RecognitionError.Line, e.RecognitionError.CharPositionInLine,
                    ErrorKind.Syntax, Level.Error, "Parsing input file: '{0}'", e.Message);
                return ExitBad(args[0]);
            }
            catch (DirectoryNotFoundException)
            {
                Write(0, 0,
                    ErrorKind.Fatal, Level.Error, "Directory '{0}' could not be found.",
                    Path.GetDirectoryName(args[0]));
                return ExitBad(args[0]);
            }
            catch (FileNotFoundException fileNotFound)
            {
                Write(0, 0,
                    ErrorKind.Fatal, Level.Error, "File '{0}' could not be found.",
                    fileNotFound.FileName);
                return ExitBad(args[0]);
            }

            if (programNode == null)
                return ExitBad(args[0]);

            Print(programNode);
            programNode.CheckSemantics();

            foreach (var item in programNode.Report)
            {
                Write(item.Line, item.Column, ErrorKind.Semantic, item.Level, item.Text);
            }

            if (programNode.Report.Level != Level.Error)
            {
                string name = Path.HasExtension(args[0]) ? 
                    Path.GetFileNameWithoutExtension(args[0]) : 
                    args[0];

                string fileName = Path.HasExtension(args[0])
                                      ? Path.ChangeExtension(Path.GetFileName(args[0]), "exe")
                                      : args[0] + ".exe";

                string directory = Path.GetDirectoryName(args[0]);

                AssemblyBuilder programAssembly = programNode.GenerateCode(
                    name,
                    fileName,
                    directory == string.Empty ? null : directory);
                programAssembly.Save(fileName);
            }
            else
                return ExitBad(args[0]);

            Flush();
            PrintBlank();
            return programNode.Report.Level != Level.Error ? 0 : 1;
        }

        static void Print(CommonTree tree)
        {
#if DEBUG
            Debug.IndentSize = 4;
            foreach (var child in PrintTree(tree))
            {
                Debug.IndentLevel = child.Item2;
                Debug.WriteLine(
                    /*(child.Item1.ChildCount != 0 ? "+ " : "  ") +*/
                    child.Item1.Text + "\t" + child.Item1.GetType()
                );
            }
            Debug.IndentLevel = 0;
#endif
        }

        static IEnumerable<Tuple<BaseTree, int>> PrintTree(BaseTree tree, int level = 0)
        {
            yield return new Tuple<BaseTree, int>(tree, level);
            if (tree.Children == null)
                yield break;

            foreach (var child in tree.Children.Cast<BaseTree>())
                foreach (var grandChild in PrintTree(child, level + 1))
                    yield return grandChild;
        }
    }
}
