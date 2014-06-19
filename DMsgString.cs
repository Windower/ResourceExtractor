// <copyright file="DMsgString.cs" company="Windower Team">
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

    internal class DMsgParser
    {
        internal static dynamic[] Parse(Stream stream, Dictionary<int, string> fields)
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

            long[] table;
            if (header.TableSize != 0)
            {
                if (header.TableSize != header.Count * 8 || header.EntrySize != 0)
                {
                    throw new InvalidOperationException("Data is corrupt.");
                }

                table = stream.ReadArray<long>(header.Count, header.HeaderSize);
            }
            else {
                if (header.DataSize != header.Count * header.EntrySize)
                {
                    throw new InvalidOperationException("Data is corrupt.");
                }

                table = new long[] { };
            }

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

            dynamic objects = new ModelObject[header.Count];

            int length = (int)header.EntrySize;
            int offset = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                if (header.TableSize != 0)
                {
                    length = (int)(table[i] >> 32);
                    offset = (int)table[i];
                }
                else
                {
                    offset = i * (int)header.EntrySize;
                }

                int count = BitConverter.ToInt32(data, offset);

                dynamic resource = new ModelObject();

                for (int j = 0; j < count; j++)
                {
                    if (!fields.ContainsKey(j))
                    {
                        continue;
                    }

                    int entryoffset = BitConverter.ToInt32(data, offset + j * 8 + 4) + offset;
                    int entrytype = BitConverter.ToInt32(data, offset + j * 8 + 8);

                    dynamic value;
                    switch (entrytype)
                    {
                    case 0:
                        entryoffset += 0x1C;

                        int stringlength = 0;
                        for (int k = 0; k < length; k++)
                        {
                            if (data[entryoffset + k] == 0)
                            {
                                break;
                            }

                            stringlength++;
                        }

                        value = ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, entryoffset, stringlength);
                        break;
                    case 1:
                        value = BitConverter.ToInt32(data, entryoffset);
                        break;
                    default:
                        value = null;
                        break;
                    }

                    resource[fields[j]] = value;
                }

                objects[i] = resource;
            }

            return objects;
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
