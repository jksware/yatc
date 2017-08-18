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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using YATC.Scope;

namespace YATC.ASTNodes
{
    internal class TypeDeclSeqNode : DeclarationNode
    {
        private LinkedList<Node<TypeDeclNode>> _topologicalSort;

        public TypeDeclSeqNode(IToken payload)
            : base(payload)
        {
        }

        public IEnumerable<TypeDeclNode> TypeDeclNodes { get { return TigerChildren.Cast<TypeDeclNode>(); } }

        /// <summary>
        /// Por pasos:
        ///     1ro : Agregar cabezas de tipos
        ///     2do : Construir grafo y revisar orden topologico
        ///     3ro : En el orden dado hacer chequeo semantico
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="report"></param>
        public override void CheckSemantics(TigerScope scope, Report report)
        {
            var checkedNodes = new Dictionary<string, TypeDeclNode>();

            // 1. agregar cabezas de tipos
            foreach (var typeDeclNode in TypeDeclNodes)
            {
                if (!checkedNodes.ContainsKey(typeDeclNode.Name))
                    checkedNodes.Add(typeDeclNode.Name, typeDeclNode);
                else
                {
                    report.AddError(this.Line, this.Column,
                        "Redeclared name in type declaration sequence: '{0}'.", typeDeclNode.Name);
                    this.IsOK = false;
                    return;
                }

                // chequeamos el header, mayormente por problemas de redeclaracion local
                // o global pero de tipos standard
                if (!typeDeclNode.CheckHeader(scope, report, string.Empty))
                {                    
                    this.IsOK = false;
                    return;
                }
            }

            // 2. DAG

            // 2.1 construir grafo

            var graph = new Dictionary<TypeDeclNode, Node<TypeDeclNode>>();

            foreach (var typeDeclNode in TypeDeclNodes)
            {
                string edgeNameTo = string.Empty;
                if (typeDeclNode.IsAliasNode)
                    edgeNameTo = ((AliasDeclNode)typeDeclNode.DeclarationNode).TypeNode.Name;
                else if (typeDeclNode.IsArrayNode)
                    edgeNameTo = ((ArrayDeclNode)typeDeclNode.DeclarationNode).ElementTypeNode.Name;

                Node<TypeDeclNode> thisNode;
                if (!graph.TryGetValue(typeDeclNode, out thisNode))
                {
                    thisNode = new Node<TypeDeclNode>(typeDeclNode);
                    graph.Add(typeDeclNode, thisNode);
                }

                if (edgeNameTo == string.Empty)
                    continue;

                TypeDeclNode edgeTo;
                if (checkedNodes.TryGetValue(edgeNameTo, out edgeTo))
                {
                    Node<TypeDeclNode> node;
                    if (!graph.TryGetValue(edgeTo, out node))
                    {
                        node = new Node<TypeDeclNode>(edgeTo);
                        graph.Add(edgeTo, node);
                    }

                    node.AddSucc(thisNode);
                }
            }

            // 2.2 obtener orden topologico (detectando ciclos)

#if DEBUG
            foreach (var edge in graph.Values)
            {
                var from = edge.Succ.Any() ? edge.Succ.FirstOrDefault().Value.IdNode.ToString() : "<end>";
                var to = edge.Value.IdNode;
                Debug.WriteLine("{0} -> {1}", from, to);
            }
#endif

            var cycleEnd = Node<TypeDeclNode>.TopologicalSort(graph.Values, out _topologicalSort);

#if DEBUG
            foreach (var node in _topologicalSort)
                Debug.WriteLine("Adding {0}", node.Value.IdNode);
#endif

            if (cycleEnd != null)
            {
                var sb = new StringBuilder();
                for (var current = cycleEnd; current != null; current = current.Parent)
                    sb.Append(current.Value.IdNode.Name + " -> ");

                sb.Append(cycleEnd.Value.IdNode.Name);
                report.AddError(this.Line, this.Column,
                                "Undetected record in recursive type declaration sequence. Cycle definition is: {0}",
                                sb.ToString());
                this.IsOK = false;
                return;
            }

            // 3. chequear semantica de los nodos en el orden topologico
            foreach (var node in _topologicalSort)
            {
                Debug.WriteLine("Checking {0}", node.Value.IdNode);
                node.Value.CheckSemantics(scope, report);
                if (!node.Value.IsOK)
                {
                    this.IsOK = false;
                    return;
                }
            }

            //foreach (var node in linkedList)
            //{
            //    node.
            //}

            this.IsOK = true;
        }

        internal override void GenerateCode(ModuleBuilder moduleBuilder)
        {
            // records go first

            IEnumerable<RecordDeclNode> recordDeclNodes =
                _topologicalSort.Select(x => x.Value).
                    Where(x => x.DeclarationNode is RecordDeclNode).
                    Select(x => x.DeclarationNode).
                    Cast<RecordDeclNode>();

            foreach (var recordDeclNode in recordDeclNodes)
                recordDeclNode.GenerateHeaderCode(moduleBuilder);

            // everything

            foreach (var declarationNode in _topologicalSort.Select(x => x.Value))
                declarationNode.GenerateCode(moduleBuilder);
        }
    }
}