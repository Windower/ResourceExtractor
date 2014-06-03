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

    internal class DMsgStringList : IList<IList<object>>
    {
        private IList<object>[] objects;

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

                objects = new IList<object>[header.Count];

                for (int i = 0; i < objects.Length; i++)
                {
                    int offset = (int)table[i];
                    int length = (int)(table[i] >> 32);

                    int count = BitConverter.ToInt32(data, offset);

                    object[] s = new string[count];

                    for (int j = 0; j < count; j++)
                    {
                        int entryoffset = BitConverter.ToInt32(data, offset + j * 8 + 4) + offset + 0x1C;
                        int entrytype = BitConverter.ToInt32(data, offset + j * 8 + 8);

                        if (entrytype == 0)
                        {
                            int stringlength = 0;
                            for (int k = 0; k < length; k++)
                            {
                                if (data[entryoffset + k] == 0)
                                {
                                    break;
                                }

                                stringlength++;
                            }

                            s[j] = ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, entryoffset, stringlength);
                        }
                        else if (entrytype == 1)
                        {
                            s[j] = BitConverter.ToUInt32(data, entryoffset);
                        }
                    }

                    objects[i] = s;
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

                objects = new IList<object>[header.Count];

                for (int i = 0; i < objects.Length; i++)
                {
                    int offset = i * (int)header.EntrySize;

                    int count = BitConverter.ToInt32(data, offset);

                    string[] s = new string[count];

                    for (int j = 0; j < count; j++)
                    {
                        int entryoffset = BitConverter.ToInt32(data, offset + j * 8 + 4);
                        int entrytype = BitConverter.ToInt32(data, offset + j * 8 + 8);

                        if (entrytype == 0)
                        {
                            entryoffset += offset + 28;

                            int length = 0;
                            for (int k = 0; k < header.EntrySize; k++)
                            {
                                if (data[entryoffset + k] == 0)
                                {
                                    break;
                                }

                                length++;
                            }

                            s[j] = ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, entryoffset, length);
                        }
                    }

                    objects[i] = s;
                }
            }
        }

        bool ICollection<IList<object>>.IsReadOnly
        {
            get { return true; }
        }

        public int Count
        {
            get { return objects.Length; }
        }

        public IList<object> this[int index]
        {
            get { return objects[index]; }

            set { throw new NotSupportedException(); }
        }

        int IList<IList<object>>.IndexOf(IList<object> item)
        {
            return ((IList<IList<object>>)objects).IndexOf(item);
        }

        bool ICollection<IList<object>>.Contains(IList<object> item)
        {
            return ((ICollection<IList<object>>)objects).Contains(item);
        }

        void ICollection<IList<object>>.CopyTo(IList<object>[] array, int arrayIndex)
        {
            objects.CopyTo(array, arrayIndex);
        }

        public IEnumerator<IList<object>> GetEnumerator()
        {
            return ((IList<IList<object>>)objects).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return objects.GetEnumerator();
        }

        public void Add(IList<object> item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, IList<object> item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(IList<object> item)
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
                get { return format; }
            }

            public bool Encrypted
            {
                get { return encrypted != 0; }
            }

            public long Version
            {
                get { return version; }
            }

            public uint HeaderSize
            {
                get { return headersize; }
            }

            public uint TableSize
            {
                get { return tablesize; }
            }

            public uint EntrySize
            {
                get { return entrysize; }
            }

            public int DataSize
            {
                get { return datasize; }
            }

            public int Count
            {
                get { return count; }
            }
        }
    }
}
