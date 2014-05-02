// <copyright file="DMsgStringList.cs" company="Windower Team">
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
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class DMsgStringList : IList<IList<string>>
    {
        private IList<string>[] strings;

        internal DMsgStringList(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            Header header = stream.Read<Header>(0);

            if (header.Format != 0x00000067736D5F64)
            {
                throw new InvalidOperationException("Invalid data format.");
            }

            if (header.Version != 0x0000000300000003)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Invalid format version. [{0:X8}]", header.Version));
            }

            if (header.TableSize != 0)
            {
                if (header.TableSize != header.Count * 8 || header.EntrySize != 0)
                {
                    throw new InvalidOperationException("Data is corrupt.");
                }

                long[] table = stream.ReadArray<long>(header.Count, header.HeaderSize);
                stream.Position = header.HeaderSize + header.TableSize;
                byte[] data = new byte[header.DataSize];
                stream.Read(data, 0, data.Length);

                if (header.Encrypted)
                {
                    for (int i = 0; i < table.Length; i++)
                    {
                        table[i] = ~table[i];
                    }

                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = (byte)~data[i];
                    }
                }

                this.strings = new IList<string>[header.Count];

                for (int i = 0; i < this.strings.Length; i++)
                {
                    int offset = (int)table[i];
                    int length = (int)(table[i] >> 32);

                    int count = BitConverter.ToInt32(data, offset);

                    string[] s = new string[count];

                    for (int j = 0; j < count; j++)
                    {
                        int stringoffset = BitConverter.ToInt32(data, offset + j * 8 + 4);
                        int stringtype = BitConverter.ToInt32(data, offset + j * 8 + 8);

                        if (stringtype == 0)
                        {
                            stringoffset += offset + 28;

                            int stringlength = 0;
                            for (int k = 0; k < length; k++)
                            {
                                if (data[stringoffset + k] == 0)
                                {
                                    break;
                                }

                                stringlength++;
                            }

                            s[j] = FF11ShiftJISDecoder.Decode(data, stringoffset, stringlength);
                        }
                    }

                    this.strings[i] = s;
                }
            }
            else
            {
                if (header.DataSize != header.Count * header.EntrySize)
                {
                    throw new InvalidOperationException("Data is corrupt.");
                }

                stream.Position = header.HeaderSize + header.TableSize;
                byte[] data = new byte[header.DataSize];
                stream.Read(data, 0, data.Length);

                if (header.Encrypted)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = (byte)~data[i];
                    }
                }

                this.strings = new IList<string>[header.Count];

                for (int i = 0; i < this.strings.Length; i++)
                {
                    int offset = i * (int)header.EntrySize;

                    int count = BitConverter.ToInt32(data, offset);

                    string[] s = new string[count];

                    for (int j = 0; j < count; j++)
                    {
                        int stringoffset = BitConverter.ToInt32(data, offset + j * 8 + 4);
                        int stringtype = BitConverter.ToInt32(data, offset + j * 8 + 8);

                        if (stringtype == 0)
                        {
                            stringoffset += offset + 28;

                            int length = 0;
                            for (int k = 0; k < header.EntrySize; k++)
                            {
                                if (data[stringoffset + k] == 0)
                                {
                                    break;
                                }

                                length++;
                            }

                            s[j] = FF11ShiftJISDecoder.Decode(data, stringoffset, length);

                            break;
                        }
                    }

                    this.strings[i] = s;
                }
            }
        }

        bool ICollection<IList<string>>.IsReadOnly
        {
            get { return true; }
        }

        public int Count
        {
            get { return this.strings.Length; }
        }

        public IList<string> this[int index]
        {
            get { return this.strings[index]; }

            set { throw new NotSupportedException(); }
        }

        int IList<IList<string>>.IndexOf(IList<string> item)
        {
            return ((IList<IList<string>>)this.strings).IndexOf(item);
        }

        bool ICollection<IList<string>>.Contains(IList<string> item)
        {
            return ((ICollection<IList<string>>)this.strings).Contains(item);
        }

        void ICollection<IList<string>>.CopyTo(IList<string>[] array, int arrayIndex)
        {
            this.strings.CopyTo(array, arrayIndex);
        }

        public IEnumerator<IList<string>> GetEnumerator()
        {
            return ((IList<IList<string>>)this.strings).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.strings.GetEnumerator();
        }

        public void Add(IList<string> item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, IList<string> item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(IList<string> item)
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

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 64)]
        private struct Header
        {
            private long format;
            private short unknown;
            private byte encrypted;
            private long version;
            private uint filesize;
            private uint headersize;
            private uint tablesize;
            private uint entrysize;
            private int datasize;
            private int count;

            public long Format
            {
                get { return this.format; }
            }

            public bool Encrypted
            {
                get { return this.encrypted != 0; }
            }

            public long Version
            {
                get { return this.version; }
            }

            public uint HeaderSize
            {
                get { return this.headersize; }
            }

            public uint TableSize
            {
                get { return this.tablesize; }
            }

            public uint EntrySize
            {
                get { return this.entrysize; }
            }

            public int DataSize
            {
                get { return this.datasize; }
            }

            public int Count
            {
                get { return this.count; }
            }
        }
    }
}
