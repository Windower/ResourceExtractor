// <copyright file="Item.cs" company="Windower Team">
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
    internal abstract class Item
    {
        private int id;

        private Strings strings;

        internal Item(byte[] data)
        {
            this.id = data[0x0] | data[0x1] << 8;
        }

        public int Id
        {
            get { return this.id; }
        }

        public string Name
        {
            get
            {
                if (this.strings != null)
                {
                    return this.strings.Name;
                }

                return string.Empty;
            }
        }

        public string LogName
        {
            get
            {
                if (this.strings != null)
                {
                    return this.strings.GetLogForm(1, null);
                }

                return string.Empty;
            }
        }

        internal static Item CreateItem(byte[] data)
        {
            data.RotateRight(5);

            int id = data[0] | data[1] << 8;

            if (id >= 0x0001 && id <= 0x0FFF)
            {
                return new GeneralItem(data);
            }
            else if (id >= 0x1000 && id <= 0x1FFF)
            {
                return new UsableItem(data);
            }
            else if (id >= 0x2000 && id <= 0x21FF)
            {
                return new AutomatonItem(data);
            }
            else if (id >= 0x2200 && id <= 0x27FF)
            {
                return new GeneralItem(data);
            }
            else if ((id >= 0x2800 && id <= 0x3FFF) || (id >= 0x6400 && id <= 0x6FFF))
            {
                return new ArmorItem(data);
            }
            else if (id >= 0x4000 && id <= 0x53FF)
            {
                return new WeaponItem(data);
            }
            else if (id >= 0x7000 && id <= 0x7400)
            {
                return new MazeItem(data);
            }
            else if (id == 0xFFFF)
            {
                return new BasicItem(data);
            }

            return new UnknownItem(data);
        }

        protected void InitializeStrings(byte[] data, int offset)
        {
            this.strings = Strings.Create(data, offset);
        }

        private sealed class UnknownItem : Item
        {
            internal UnknownItem(byte[] data)
                : base(data)
            {
            }
        }
    }
}
