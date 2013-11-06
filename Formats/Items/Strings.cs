// <copyright file="Strings.cs" company="Windower Team">
// Copyright © 2013 Windower Team
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

namespace ResourceExtractor.Formats.Items
{
    using System.Globalization;
    using ResourceExtractor;

    internal abstract class Strings
    {
        private string name;

        internal Strings(byte[] data, int offset)
        {
            this.name = (string)DecodeEntry(data, offset, 0);
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public abstract string GetLogForm(int count, bool? definite);

        internal static Strings Create(byte[] data, int offset)
        {
            int count = data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;

            switch (count)
            {
                case 2: return new JapaneseStrings(data, offset);
                case 5: return new EnglishStrings(data, offset);
                case 6: return new FrenchStrings(data, offset);
                case 9: return new GermanStrings(data, offset);
            }

            return new UnknownStrings(data, offset);
        }

        protected static object DecodeEntry(byte[] data, int offset, int index)
        {
            int entryoffset = offset + index * 8 + 4;
            int dataoffset = data[entryoffset] | data[entryoffset + 1] << 8 | data[entryoffset + 2] << 16 | data[entryoffset + 3] << 24;
            int datatype = data[entryoffset + 4] | data[entryoffset + 5] << 8 | data[entryoffset + 6] << 16 | data[entryoffset + 7] << 24;

            dataoffset += offset;

            switch (datatype)
            {
                case 0:
                    int length = 0;

                    for (int i = dataoffset + 28; i < data.Length; i++)
                    {
                        if (data[i] == 0)
                        {
                            break;
                        }

                        length++;
                    }

                    return FF11ShiftJISDecoder.Decode(data, dataoffset + 28, length);
                case 1:
                    return data[dataoffset] | data[dataoffset + 1] << 8 | data[dataoffset + 2] << 16 | data[dataoffset + 3] << 24;
            }

            return null;
        }

        private class UnknownStrings : Strings
        {
            internal UnknownStrings(byte[] data, int offset)
                : base(data, offset)
            {
            }

            public override string GetLogForm(int count, bool? definite)
            {
                if (count != 1)
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}×{1}", count, this.Name);
                }

                return this.Name;
            }
        }
    }
}
