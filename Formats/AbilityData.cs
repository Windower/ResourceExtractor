// <copyright file="AbilityData.cs" company="Windower Team">
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

    internal class AbilityData
    {
        private int id;
        private AbilityType type;
//        private byte[] firstbytes = new byte[3];
        private int mpcost;
        private int timerid;
        private ValidTargets validtargets;
        private int tpcost;
        private int monsterlevel;
//        private byte[] otherbytes = new byte[35];

        private AbilityData(byte[] data)
        {
            byte b2 = data[2];
            byte b11 = data[11];
            byte b12 = data[12];

            data.Decode();

            data[2] = b2;
            data[11] = b11;
            data[12] = b12;

            this.id = data[0] | data[1] << 8;
            this.type = (AbilityType)data[2];
//            Array.Copy(data, 3, this.firstbytes, 0, this.firstbytes.Length);
            this.mpcost = data[6] | data[7] << 8;
            this.timerid = data[8] | data[9] << 8;
            this.validtargets = (ValidTargets)(data[10] | data[11] << 8);
            this.tpcost = data[12];
            this.monsterlevel = data[15];
//            Array.Copy(data, 13, this.otherbytes, 0, this.otherbytes.Length);
        }

        public int Id
        {
            get { return this.id; }
        }

        public AbilityType AbilityType
        {
            get { return this.type; }
        }

        public int MPCost
        {
            get { return this.mpcost; }
        }

        public int TimerId
        {
            get { return this.timerid; }
        }

        public ValidTargets ValidTargets
        {
            get { return this.validtargets; }
        }

        public int TPCost
        {
            get { return this.tpcost == 255 ? 0 : this.tpcost; }
        }

        public int MonsterLevel
        {
            get { return this.monsterlevel; }
        }

//        public byte[] Firstbytes
//        {
//            get
//            {
//                return this.firstbytes;
//            }
//        }

//        public byte[] Otherbytes
//        {
//            get
//            {
//                return this.otherbytes;
//            }
//        }

        public static IList<AbilityData> Load(Stream stream, int length)
        {
            IList<AbilityData> result = new List<AbilityData>();

            byte[] buffer = new byte[0x30];

            length /= buffer.Length;
            for (int i = 0; i < length; i++)
            {
                stream.Read(buffer, 0, buffer.Length);
                result.Add(new AbilityData(buffer));
            }

            return result;
        }
    }
}
