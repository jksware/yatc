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

using System.Collections;
using System.Collections.Generic;

namespace YATC.ASTNodes
{
    /// <summary>
    /// Indica cuán mal es el error
    /// </summary>
    public enum Level { Info = 0, Warning = 1, Error = 2 }

    public class Report : IEnumerable<Item>
    {
        protected readonly List<Item> Items = new List<Item>();
        public Level Level { get; private set; }
        public bool IsOK { get { return Level != Level.Error; } }

        public void AddError(int line, int column, string text, params object[] modifiers)
        {
            if (Level < Level.Error)
                Level = Level.Error;
            Items.Add(new Item(Level.Error, line, column, string.Format(text, modifiers)));
        }

        public void AddWarning(int line, int column, string text, params object[] modifiers)
        {
            if (Level < Level.Warning)
                Level = Level.Warning;
            Items.Add(new Item(Level.Warning, line, column, string.Format(text, modifiers)));
        }

        public void AddInfo(int line, int column, string text, params object[] modifiers)
        {
            if (Level < Level.Info)
                Level = Level.Info;
            Items.Add(new Item(Level.Info, line, column, string.Format(text, modifiers)));
        }

        public void Reset()
        {
            Level = Level.Info;
            Items.Clear();
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
