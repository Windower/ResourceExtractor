// <copyright file="DialogString.cs" company="Windower Team">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    internal class DialogString
    {
        internal static string[] Parse(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            Header header = stream.Read<Header>(0);

            if (header.FileSize != stream.Length - 4)
            {
                throw new InvalidOperationException("Data is corrupt.");
            }

            var table = stream.ReadArray<uint>(header.TableSize);
            for (var i = 0; i < table.Length; ++i)
            {
                table[i] ^= 0x80;
            }

            var objects = new string[header.TableSize / 4];

            for (var i = 0; i < table.Length; ++i)
            {
                var index = table[i];
                var length = (i + 1 < table.Length ? table[i + 1] : stream.Length) - table[i];
                var data = stream.ReadArray<byte>((int)length);
                for (var j = 0; j < data.Length; ++j)
                {
                    data[j] ^= 0x80;
                }
            }

            return objects;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
        private struct Header
        {
            private ushort fileSize;
            private ushort tableSize;

            public ushort FileSize
            {
                get
                {
                    return fileSize;
                }
            }

            public ushort TableSize
            {
                get
                {
                    return tableSize;
                }
            }
        }
    }
}
