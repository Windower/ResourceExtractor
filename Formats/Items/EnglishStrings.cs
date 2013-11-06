// <copyright file="EnglishStrings.cs" company="Windower Team">
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

    internal class EnglishStrings : Strings
    {
        private string lognamesingular;
        private string lognameplural;

        private int articleid;

        internal EnglishStrings(byte[] data, int offset)
            : base(data, offset)
        {
            this.articleid = (int)DecodeEntry(data, offset, 1);
            this.lognamesingular = (string)DecodeEntry(data, offset, 2);
            this.lognameplural = (string)DecodeEntry(data, offset, 3);
        }

        public override string GetLogForm(int count, bool? definite)
        {
            if (definite == true)
            {
                if (count != 1)
                {
                    return string.Format(CultureInfo.GetCultureInfo("en-US"), "the {0:#,0} {1}", count, this.lognameplural);
                }

                return string.Format(CultureInfo.GetCultureInfo("en-US"), "the {0}", this.lognamesingular);
            }
            else
            {
                if (count != 1)
                {
                    return string.Format(CultureInfo.GetCultureInfo("en-US"), "{0:#,0} {1}", count, this.lognameplural);
                }

                if (definite == false)
                {
                    switch (this.articleid)
                    {
                        case 0: return string.Format(CultureInfo.GetCultureInfo("en-US"), "a {0}", this.lognamesingular);
                        case 1: return string.Format(CultureInfo.GetCultureInfo("en-US"), "an {0}", this.lognamesingular);
                    }
                }
            }

            return this.lognamesingular;
        }
    }
}
