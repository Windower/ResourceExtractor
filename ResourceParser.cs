// <copyright file="Program.cs" company="Windower Team">
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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Dynamic;
    using System.IO;
    using System.Text;

    static class ResourceParser
    {
        public static IList<dynamic> ParseAbilities(Stream stream, int length)
        {
            var abilities = new List<dynamic>();

            var data = new byte[0x30];
            for (var i = 0; i < length / data.Length; ++i)
            {
                stream.Read(data, 0, data.Length);
                byte b2 = data[2];
                byte b11 = data[11];
                byte b12 = data[12];

                data.Decode();

                data[2] = b2;
                data[11] = b11;
                data[12] = b12;

                dynamic ability = new ExpandoObject();

                using (MemoryStream mstream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(mstream, Encoding.ASCII, true))
                {
                    ability.id = reader.ReadInt16();
                    ability.type = (AbilityType) reader.ReadByte();
                    reader.ReadBytes(0x03);     // Unknown 03 - 05
                                                // 03: Element/Icon ID
                                                // 04: Related to skill... h2h WS are in the 75-79 range, dagger WS in the 80-85 range, etc.
                    ability.mp_cost = reader.ReadInt16();
                    ability.recast_id = reader.ReadInt16();
                    ability.targets = reader.ReadInt16();
                    var tp_cost = reader.ReadSByte();   // This is probably two bytes long
                    ability.tp_cost = tp_cost == -1 ? 0 : tp_cost;
                    reader.ReadBytes(0x02);     // Unknown 0D - 0E
                    ability.monster_level = reader.ReadByte();

                    // Derived data
                    ability.prefix = ((AbilityType) ability.type).Prefix();
                }

                abilities.Add(ability);
            }

            return abilities;
        }

        public static IList<dynamic> ParseSpells(Stream stream, int length)
        {
            var spells = new List<dynamic>();

            var data = new byte[0x40];
            for (var i = 0; i < length / data.Length; ++i)
            {
                stream.Read(data, 0, data.Length);
                byte b2 = data[2];
                byte b11 = data[11];
                byte b12 = data[12];

                data.Decode();

                data[2] = b2;
                data[11] = b11;
                data[12] = b12;

                bool valid = data[40] != 0;
                // Check if spell is usable by any job.
                for (int j = 0; j < 24; ++j)
                {
                    valid |= data[14 + j] != 0xFF;
                }

                // Invalid spell
                if (!valid)
                {
                    continue;
                }

                dynamic spell = new ExpandoObject();

                using (MemoryStream mstream = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(mstream, Encoding.ASCII, true))
                {
                    spell.id = reader.ReadInt16();
                    spell.type = (MagicType) reader.ReadInt16();
                    spell.element = reader.ReadByte();
                    reader.ReadByte();          // Unknown 05 - 05, possibly just padding or element being a short
                    spell.targets = reader.ReadInt16();
                    spell.skill = reader.ReadInt16();
                    spell.mp_cost = reader.ReadInt16();
                    spell.cast_time = reader.ReadByte();
                    spell.recast = reader.ReadByte();
                    var levels = reader.ReadBytes(0x18);
                    spell.recast_id = reader.ReadInt16();
                    spell.icon_id = reader.ReadByte();

                    // Derived data
                    spell.prefix = ((MagicType) spell.type).Prefix();
                    switch ((byte) spell.icon_id)
                    {
                    case 56:
                    case 57:
                    case 58:
                    case 59:
                    case 60:
                    case 61:
                    case 62:
                    case 63:
                        spell.element = spell.icon_id - 56;
                        break;
                    case 64:
                        spell.element = -1;
                        break;
                    }

                    spell.levels = new Dictionary<int, int>();
                    // Discard last entry, always a copy of white mages...
                    for (var j = 0; j < levels.Length - 1; ++j)
                    {
                        if (levels[j] != 0xFF)
                        {
                            spell.levels[j] = levels[j];
                        }
                    }
                }

                spells.Add(spell);
            }

            return spells;
        }

        public static IList<dynamic> ParseItems(Stream stream, Stream streamja, Stream streamde, Stream streamfr)
        {
            var items = new List<dynamic>();

            byte[] data = new byte[0x200];
            byte[] dataja = new byte[0x200];
            byte[] datade = new byte[0x200];
            byte[] datafr = new byte[0x200];
            int count = (int) (stream.Length / 0xC00);
            for (int i = 0; i < count; i++)
            {
                streamfr.Position = streamde.Position = streamja.Position = stream.Position = i * 0xC00;
                stream.Read(data, 0, data.Length);
                streamja.Read(dataja, 0, dataja.Length);
                streamde.Read(datade, 0, datade.Length);
                streamfr.Read(datafr, 0, datafr.Length);

                dynamic item = new ExpandoObject();

                data.RotateRight(5);
                dataja.RotateRight(5);
                datade.RotateRight(5);
                datafr.RotateRight(5);

                using (
                    Stream stringstream = new MemoryStream(data),
                           stringstreamja = new MemoryStream(dataja),
                           stringstreamde = new MemoryStream(datade),
                           stringstreamfr = new MemoryStream(datafr))
                using (BinaryReader reader = new BinaryReader(stringstream, Encoding.ASCII, true))
                {
                    item.id = reader.ReadUInt16();
                    item.category = "General";

                    if (item.id >= 0x0001 && item.id <= 0x0FFF || item.id >= 0x2200 && item.id < 0x2800)
                    {
                        ParseGeneralItem(reader, item);
                    }
                    else if (item.id >= 0x1000 && item.id < 0x2000)
                    {
                        ParseUsableItem(reader, item);
                    }
                    else if (item.id >= 0x2000 && item.id < 0x2200)
                    {
                        ParseAutomatonItem(reader, item);
                    }
                    else if ((item.id >= 0x2800 && item.id < 0x4000) || (item.id >= 0x6400 && item.id < 0x7000))
                    {
                        ParseArmorItem(reader, item);
                        item.category = "Armor";
                    }
                    else if (item.id >= 0x4000 && item.id < 0x5400)
                    {
                        ParseWeaponItem(reader, item);
                        item.category = "Weapon";
                    }
                    else if (item.id >= 0x7000 && item.id < 0x7400)
                    {
                        ParseMazeItem(reader, item);
                    }
                    //else if (item.id >= 0xF000 && item.id < 0xF200)
                    //{
                    //    ParseMonstrosityItem(reader, item);
                    //}
                    else if (item.id == 0xFFFF)
                    {
                        ParseBasicItem(reader, item);
                    }

                    if (stringstream.Position > 0x02)
                    {
                        stringstreamfr.Position = stringstreamde.Position = stringstreamja.Position = stringstream.Position;

                        ParseItemString(reader, item);
                        ParseItemString(new BinaryReader(stringstreamja), item);
                        ParseItemString(new BinaryReader(stringstreamde), item);
                        ParseItemString(new BinaryReader(stringstreamfr), item);

                        items.Add(item);
                    }
                }
            }

            return items;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801")]
        static private void ParseBasicItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0E);             // Unknown 02 - 0F
        }
        static private void ParseGeneralItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.targets = reader.ReadInt16();

            reader.ReadBytes(0x0A);             // Unknown 0E - 17
        }
        static private void ParseWeaponItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.targets = reader.ReadUInt16();
            item.level = reader.ReadUInt16();
            item.slots = reader.ReadUInt16();
            item.races = reader.ReadUInt16();
            item.jobs = reader.ReadUInt32();
            reader.ReadBytes(0x0D);             // Unknown 18 - 24
            item.cast_time = reader.ReadByte();
            reader.ReadBytes(0x02);             // Unknown 26 - 27
            item.recast = reader.ReadUInt32();
            reader.ReadBytes(0x04);             // Unknown 2C - 2F
        }
        static private void ParseArmorItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.targets = reader.ReadUInt16();
            item.level = reader.ReadUInt16();
            item.slots = reader.ReadUInt16();
            item.races = reader.ReadUInt16();
            item.jobs = reader.ReadUInt32();
            reader.ReadBytes(0x03);             // Unknown 18 - 1A
            item.cast_time = reader.ReadByte();
            reader.ReadBytes(0x04);             // Unknown 1C - 1F
            item.recast = reader.ReadUInt32();
            reader.ReadBytes(0x04);             // Unknown 24 - 27
        }
        static private void ParseUsableItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.targets = reader.ReadUInt16();
            item.cast_time = reader.ReadUInt16();
            reader.ReadBytes(0x08);             // Unknown 10 - 17
        }
        [SuppressMessage("Microsoft.Usage", "CA1801")]
        static private void ParseAutomatonItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x16);             // Unknown 02 - 17
        }
        [SuppressMessage("Microsoft.Usage", "CA1801")]
        static private void ParseMazeItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x52);             // Unknown 02 - 53
        }
        static private void ParseMonstrosityItem(BinaryReader reader, dynamic item)
        {
            item.tp_moves = new Dictionary<ushort, sbyte>();
            reader.ReadBytes(0x2E);             // Unknown 02 - 2F
            for (var i = 0x00; i < 0x10; ++i)
            {
                var move = reader.ReadUInt16();
                var level = reader.ReadSByte();
                if (level != 0 && level != -1 && !item.TPMoves.ContainsKey(move))
                {
                    item.tp_moves.Add(move, level);
                }
                reader.ReadByte();              // Unknown byte, possibly padding, or level being a short
            }
        }

        private enum StringIndex
        {
            Name = 0,
            EnglishArticle = 1,
            EnglishLogSingular = 2,
            EnglishLogPlural = 3,
            FrenchGender = 1,
            FrenchArticle = 2,
            FrenchLogSingular = 3,
            FrenchLogPlural = 4,
            GermanLogSingular = 4,
            GermanLogPlural = 7,
        }
        static private void ParseItemString(BinaryReader reader, dynamic item)
        {
            var language = reader.ReadInt32();
            switch (language)
            {
            // Japanese
            case 2:
                item.ja = DecodeEntry(reader, StringIndex.Name);
                item.jal = item.ja;
                break;

            // English
            case 5:
                item.en = DecodeEntry(reader, StringIndex.Name);
                item.enl = DecodeEntry(reader, StringIndex.EnglishLogSingular);
                break;

            // French
            case 6:
                item.fr = DecodeEntry(reader, StringIndex.Name);
                item.frl = DecodeEntry(reader, StringIndex.FrenchLogSingular);
                break;

            // German
            case 9:
                item.de = DecodeEntry(reader, StringIndex.Name);
                item.del = DecodeEntry(reader, StringIndex.GermanLogSingular);
                break;

            // Shouldn't happen, suggests new format (or new language)
            default:
                Stream stream = reader.BaseStream;
                stream.Position = 0;
                Console.WriteLine(String.Format("Unknown language format. Item: {0}, Language (#fields): {1}, Data:\n{2}", item.id, language, BitConverter.ToString(reader.ReadBytes((int) stream.Length)).Replace("-", " ")));
                break;
            }
        }
        static private object DecodeEntry(BinaryReader reader, StringIndex index)
        {
            Stream stream = reader.BaseStream;
            long origin = stream.Position;
            reader.ReadBytes(8 * (int) index);
            int dataoffset = reader.ReadInt32();
            int datatype = reader.ReadInt32();
            stream.Position = origin;

            reader.ReadBytes(dataoffset);

            switch (datatype)
            {
            case 0:
                reader.ReadBytes(0x18);
                long dataorigin = stream.Position;
                int length;

                while (stream.Position != stream.Length && reader.ReadByte() != 0);
                length = (int) (stream.Position - dataorigin) - 1;
                stream.Position = dataorigin;

                var res = FF11ShiftJISDecoder.Decode(reader.ReadBytes(length), 0, length);
                stream.Position = origin;
                return res;

            case 1:
                return reader.ReadInt32();
            }

            return null;
        }
    }
}
