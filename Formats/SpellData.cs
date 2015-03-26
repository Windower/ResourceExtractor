// <copyright file="SpellData.cs" company="Windower Team">
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

namespace ResourceExtractor.Formats
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class SpellData
    {
        private int index;
        private MagicType magictype;
        private Element element;
        private ValidTargets validtargets;
        private Skill skill;
        private int mpcost;
        private int casttime;
        private int recast;
        private int[] levels = new int[24];
        private int id;
        private int iconid;
        public int duration;

        private bool valid;

        private SpellData(byte[] data)
        {
            byte b2 = data[2];
            byte b11 = data[11];
            byte b12 = data[12];

            data.Decode();

            data[2] = b2;
            data[11] = b11;
            data[12] = b12;

            this.index = data[0] | data[1] << 8;
            this.magictype = (MagicType)(data[2] | data[3] << 8);
            this.element = (Element)(sbyte)data[4];
            this.validtargets = (ValidTargets)(data[6] | data[7] << 8);
            this.skill = (Skill)(data[8] | data[9] << 8);
            this.mpcost = (data[10] | data[11] << 8);
            this.casttime = data[12];
            this.recast = data[13];
            for (var i = 0; i < 24; ++i)
            {
                this.levels[i] = (short)(data[14 + i] << 8 | data[15 + i]);
            }
            this.id = data[62] | data[63] << 8;
            this.iconid = data[64];

            this.valid = this.iconid != 0;
            // Check if spell is usable by any job.
            for (int i = 0; i < 24; ++i)
            {
                this.valid |= this.levels[i] != -1;
            }

            duration = 0;
        }

        public int Index
        {
            get
            {
                return this.index;
            }
        }

        public MagicType MagicType
        {
            get
            {
                return this.magictype;
            }
        }

        public Element Element
        {
            get
            {
                return this.element;
            }
        }

        public ValidTargets ValidTargets
        {
            get
            {
                return this.validtargets;
            }
        }

        public Skill Skill
        {
            get
            {
                return this.skill;
            }
        }

        public int MPCost
        {
            get
            {
                return this.mpcost;
            }
        }

        public float CastTime
        {
            get
            {
                return this.casttime / 4f;
            }
        }

        public float Recast
        {
            get
            {
                return this.recast / 4f;
            }
        }

        public int Id
        {
            get
            {
                return this.id;
            }
        }

        public int IconId
        {
            get
            {
                return this.iconid;
            }
        }

        public bool Valid
        {
            get
            {
                return this.valid;
            }
        }

        public int[] Levels
        {
            get
            {
                return this.levels;
            }
        }

        public static IList<SpellData> Load(Stream stream, int length)
        {
            IList<SpellData> result = new List<SpellData>();

            byte[] buffer = new byte[0x58];

            length /= buffer.Length;
            for (int i = 0; i < length; i++)
            {
                stream.Read(buffer, 0, buffer.Length);
                result.Add(new SpellData(buffer));
            }

            return result;
        }
    }
}
