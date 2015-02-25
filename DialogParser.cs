// <copyright file="DialogParser.cs" company="Windower Team">
// Copyright © 2014 Windower Team
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
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class DialogParser
    {
        internal static dynamic[] Parse(Stream stream, string key)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            Header header = stream.Read<Header>(0);
            // First valid value was included in the header to get the table size
            stream.Position -= 4;

            if (header.FileSize != stream.Length - 4)
            {
                throw new InvalidOperationException("Data is corrupt.");
            }

            var data = new byte[header.FileSize];
            stream.Read(data, 0, data.Length);
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] ^= 0x80;
            }
            int[] table;
            using (var datastream = new MemoryStream(data))
            {
                table = datastream.ReadArray<int>((int)header.TableSize);
            }

            dynamic objects = new ModelObject[header.TableSize];

            for (var i = 0; i < table.Length; ++i)
            {
                var offset = table[i];
                var length = (int)(i + 1 < table.Length ? table[i + 1] : data.Length) - offset;

                for (; data[offset + length - 1] == 0; --length) ;

                objects[i] = new ModelObject {
                    {key, ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, offset, length)}
                };
            }

            return objects;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
        private struct Header
        {
            private int fileSize;
            private int tableSize;

            public int FileSize
            {
                get
                {
                    return fileSize - 0x10000000;
                }
            }

            public int TableSize
            {
                get
                {
                    return (int)(tableSize ^ 0x80808080) / 4;
                }
            }
        }
    }
}
