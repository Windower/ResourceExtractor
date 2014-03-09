// <copyright file="GermanStrings.cs" company="Windower Team">
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

namespace ResourceExtractor.Formats.Items
{
    using System.Globalization;

    internal class GermanStrings : Strings
    {
        private string lognamesingular;
        private string lognameplural;

        internal GermanStrings(byte[] data, int offset)
            : base(data, offset)
        {
            this.lognamesingular = (string)DecodeEntry(data, offset, 4);
            this.lognameplural = (string)DecodeEntry(data, offset, 7);
        }

        public override string GetLogForm(int count, bool? definite)
        {
            // TODO: This method is almost entirely unimplemented, only a very basic implementation is provided.

            if (count != 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} {1}", count, this.lognameplural);
            }

            return this.lognamesingular;
        }
    }
}
