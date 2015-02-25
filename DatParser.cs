// <copyright file="DatParser.cs" company="Windower Team">
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
    using System.Text;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class DatParser
    {
        private DatParser() { }

        public static dynamic[] Parse(Stream stream, Dictionary<int, string> fields)
        {
            var format = stream.Read<ulong>();
            stream.Position = 0;

            // DMsg format
            if (format == 0x00000067736D5F64)
            {
                return DMsgParser.Parse(stream, fields);
            }

            // Dialog format
            if ((format & 0x10000000) > 0 && format % 0x10000000 == (ulong) stream.Length - 4)
            {
                return DialogParser.Parse(stream, fields[0]);
            }

            // Auto-translate files (no specific format)
            if ((format & 0xFFFFFCFF) == 0x00010002)
            {
                return ATParser.Parse(stream, fields[0]);
            }

            throw new InvalidDataException("Unknown DAT format.");
        }
    }
}
