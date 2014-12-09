// <copyright file="WeaponItem.cs" company="Windower Team">
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
    internal sealed class WeaponItem : EquippableItem
    {
        private ValidTargets validtargets;
        private int level;
        private int slots;
        private int races;
        private int jobs;
        private int casttime;
        private int recast;

        internal WeaponItem(byte[] data)
            : base(data)
        {
            this.validtargets = (ValidTargets)(data[0x0C] | data[0x0D] << 8);
            this.level = data[0x0E] | data[0x0F] << 8;
            this.slots = data[0x10] | data[0x11] << 8;
            this.races = data[0x12] | data[0x13] << 8;
            this.jobs = data[0x14] | data[0x15] << 8 | data[0x16] << 16 | data[0x17] << 24;
            this.casttime = data[0x25];
            this.recast = data[0x28] | data[0x29] << 8 | data[0x2A] << 16 | data[0x2B] << 24;

            this.InitializeStrings(data, 0x34);
        }

        public override ValidTargets ValidTargets
        {
            get { return this.validtargets; }
        }

        public override int Level
        {
            get { return this.level; }
        }

        public override int Slots
        {
            get { return this.slots; }
        }

        public override int Races
        {
            get { return this.races; }
        }

        public override int Jobs
        {
            get { return this.jobs; }
        }

        public override float CastTime
        {
            get { return this.casttime / 4f; }
        }

        public override float Recast
        {
            get { return this.recast; }
        }
    }
}
