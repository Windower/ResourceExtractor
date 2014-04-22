// <copyright file="Container.cs" company="Windower Team">
// Copyright © 2013-2014 Windower Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
// </copyright>

namespace ResourceExtractor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class Container : IList<object>
    {
        private IList<object> list;

        public Container(Stream stream)
        {
            list = new List<object>();

            Header header = stream.Read<Header>();
            long block = stream.Position;

            if (header.Type != BlockType.ContainerBegin)
            {
                throw new InvalidDataException();
            }

            stream.Position = block + header.Size;

            while (header.Type != BlockType.ContainerEnd)
            {
                header = stream.Read<Header>();
                block = stream.Position;

                object o = Load(stream, header);
                if (o != null)
                {
                    list.Add(o);
                }

                stream.Position = block + header.Size;
            }
        }

        private enum BlockType
        {
            ContainerEnd = 0x00,
            ContainerBegin = 0x01,
            SpellData = 0x49,
            AbilityData = 0x53,
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public int Count
        {
            get { return list.Count; }
        }

        public object this[int index]
        {
            get
            {
                return list[index];
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public bool Contains(object item)
        {
            return list.Contains(item);
        }

        public int IndexOf(object item)
        {
            return list.IndexOf(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<object> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }

        public void Add(object item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(object item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, object item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        private static object Load(Stream stream, Header header)
        {
            switch (header.Type)
            {
                case BlockType.ContainerEnd:
                    break;
                case BlockType.ContainerBegin:
                    stream.Position -= Marshal.SizeOf(typeof(Header));
                    return new Container(stream);
                case BlockType.SpellData:
                    return new KeyValuePair<string, object>("spells", ResourceParser.ParseSpells(stream, header.Size));
                case BlockType.AbilityData:
                    return new KeyValuePair<string, object>("abilities", ResourceParser.ParseAbilities(stream, header.Size));
                default:
                    Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unknown [{0:X2}]", (int)header.Type));
                    break;
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            private int id;
            private int size;
            private long padding;

            public int Id
            {
                get { return id; }
            }

            public int Size
            {
                get { return (int)(((uint)size >> 3) & ~0xF) - 16; }
            }

            public BlockType Type
            {
                get { return (BlockType)(size & 0x7F); }
            }
        }
    }
}
