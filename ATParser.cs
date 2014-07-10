// <copyright file="ATParser.cs" company="Windower Team">
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
    internal class ATParser
    {
        internal static dynamic[] Parse(Stream stream, string key)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var objects = new List<ModelObject>();

            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    reader.ReadByte();
                    var language = reader.ReadByte();
                    int id = 0x100 * reader.ReadByte() | reader.ReadByte();

                    dynamic entry = new ModelObject();
                    entry.id = id;

                    string name;

                    if ((id & 0xFF) == 0)
                    {
                        var bracket_name_data = reader.ReadBytes(0x20);
                        var name_data = reader.ReadBytes(0x20);
                        var phrases = reader.ReadInt32();
                        var block_size = reader.ReadInt32();

                        var length = 0;
                        for (; name_data[length] != 0x00; ++length);

                        name = ShiftJISFF11Encoding.ShiftJISFF11.GetString(name_data, 0, length);
                    }
                    else
                    {
                        var length = reader.ReadByte();
                        name = ShiftJISFF11Encoding.ShiftJISFF11.GetString(reader.ReadBytes(length), 0, length - 1);

                        if (language == 1)
                        {
                            reader.ReadBytes(reader.ReadByte());
                        }
                    }

                    entry[key] = name;

                    objects.Add(entry);
                }
            }

            return objects.ToArray();
        }
    }
}
