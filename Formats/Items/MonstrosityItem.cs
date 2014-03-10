// <copyright file="MonstrosityItem.cs" company="Windower Team">
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
    using System;
    using System.Collections.Generic;

    internal sealed class MonstrosityItem : Item
    {
        private IDictionary<int, int> tpmoves = new Dictionary<int, int>();

        internal MonstrosityItem(byte[] data)
            : base(data)
        {
            this.InitializeStrings(data, 0x70);

            for (int i = 0; i < 15; i++)
            {
                int offset = i * 4;
                if (data[48 + offset + 2] != 0 & data[48 + offset + 2] != 0xFF & !tpmoves.ContainsKey(data[48 + offset] | data[48 + offset + 1] << 8))
                {
                    tpmoves.Add(data[48 + offset] | data[48 + offset + 1] << 8, data[48 + offset + 2]);
                }
            }

        }

        public IDictionary<int,int> TPMoves
        {
            get
            {
                return this.tpmoves;
            }
        }
    }
}
