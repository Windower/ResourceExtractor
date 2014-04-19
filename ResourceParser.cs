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
        public static IList<ExpandoObject> ParseAbilities(Stream stream, int length)
        {
            var abilities = new List<ExpandoObject>();

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

                ability.ID = data[0] | data[1] << 8;
                ability.Type = (AbilityType) data[2];
                ability.Prefix = ability.Type.Prefix();
                ability.MPCost = data[6] | data[7] << 8;
                ability.RecastID = data[8] | data[9] << 8;
                ability.Targets = (ValidTargets) (data[10] | data[11] << 8);
                ability.TPCost = data[12];
                ability.MonsterLevel = data[15];
                ability.Skill = Skill.None;
                ability.Element = Element.None;

                ability.Valid = true;

                abilities.Add(ability);
            }

            return abilities;
        }

        public static IList<ExpandoObject> ParseSpells(Stream stream, int length)
        {
            var spells = new List<ExpandoObject>();

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

                spell.RecastID = BitConverter.ToInt16(data, 0);
                spell.Type = (MagicType) BitConverter.ToInt16(data, 2);
                spell.Prefix = spell.Type.Prefix();
                spell.Targets = (ValidTargets) BitConverter.ToInt16(data, 6);
                spell.Skill = (Skill) BitConverter.ToInt16(data, 8);
                spell.MPCost = BitConverter.ToInt16(data, 10);
                spell.CastTime = data[12];
                spell.Recast = data[13];
                spell.Levels = new byte[24];
                Array.Copy(data, 14, spell.Levels, 0, spell.Levels.Length);
                spell.ID = BitConverter.ToInt16(data, 38);
                spell.IconID = data[40];
                spell.Element = (Element) (spell.IconID == 64 ? -1 : spell.IconID - 56);

                spells.Add(spell);
            }

            return spells;
        }

        public static IList<ExpandoObject> ParseItems(Stream stream, Stream streamja, Stream streamde, Stream streamfr)
        {
            var items = new List<ExpandoObject>();

            byte[] data = new byte[0x200];
            byte[] dataja = new byte[0x200];
            byte[] datade = new byte[0x200];
            byte[] datafr = new byte[0x200];
            int count = (int) (stream.Length / 0xC00);
            for (int i = 0; i < count; i++)
            {
                stream.Position = i * 0xC00;
                stream.Read(data, 0, data.Length);
                streamja.Position = i * 0xC00;
                streamja.Read(dataja, 0, dataja.Length);
                streamde.Position = i * 0xC00;
                streamde.Read(datade, 0, datade.Length);
                streamfr.Position = i * 0xC00;
                streamfr.Read(datafr, 0, datafr.Length);

                dynamic item = new ExpandoObject();

                data.RotateRight(5);
                dataja.RotateRight(5);
                datade.RotateRight(5);
                datafr.RotateRight(5);

                item.ID = data[0] | data[1] << 8;
                item.Category = "General";

                using (
                    Stream stringstream   = new MemoryStream(data),
                           stringstreamja = new MemoryStream(dataja),
                           stringstreamde = new MemoryStream(datade),
                           stringstreamfr = new MemoryStream(datafr))
                {

                    if (item.ID >= 0x0001 && item.ID <= 0x0FFF || item.ID >= 0x2200 && item.ID < 0x2800)
                    {
                        ParseGeneralItem(stringstream, item);
                    }
                    else if (item.ID >= 0x1000 && item.ID < 0x2000)
                    {
                        ParseUsableItem(stringstream, item);
                    }
                    else if (item.ID >= 0x2000 && item.ID < 0x2200)
                    {
                        ParseAutomatonItem(stringstream, item);
                    }
                    else if ((item.ID >= 0x2800 && item.ID < 0x4000) || (item.ID >= 0x6400 && item.ID < 0x7000))
                    {
                        ParseArmorItem(stringstream, item);
                        item.Category = "Armor";
                    }
                    else if (item.ID >= 0x4000 && item.ID < 0x5400)
                    {
                        ParseWeaponItem(stringstream, item);
                        item.Category = "Weapon";
                    }
                    else if (item.ID >= 0x7000 && item.ID < 0x7400)
                    {
                        ParseMazeItem(stringstream, item);
                    }
                    else if (item.ID >= 0xF000 && item.ID < 0xF200)
                    {
                        ParseMonstrosityItem(stringstream, item);
                    }
                    else if (item.ID == 0xFFFF)
                    {
                        ParseBasicItem(stringstream, item);
                    }

                    if (stringstream.Position > 0x00)
                    {
                        stringstreamfr.Position = stringstreamde.Position = stringstreamja.Position = stringstream.Position;
                        ParseItemString(stringstream, item);
                        ParseItemString(stringstreamja, item);
                        ParseItemString(stringstreamde, item);
                        ParseItemString(stringstreamfr, item);
                    }

                    items.Add(item);
                }
            }

            return items;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801")]
        static private void ParseBasicItem(Stream stream, ref dynamic item)
        {
            stream.Position += 0x10;
        }
        static private void ParseGeneralItem(Stream stream, ref dynamic item)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Position += 0x0C;            // Unknown 00 - 0B
                item.ValidTargets = (ValidTargets) reader.ReadInt16();

                stream.Position += 0x0A;
            }
        }
        static private void ParseWeaponItem(Stream stream, ref dynamic item)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Position += 0x0C;            // Unknown 00 - 0B
                item.ValidTargets = (ValidTargets) reader.ReadInt16();
                item.Level = reader.ReadInt16();
                item.Slots = reader.ReadInt16();
                item.Races = reader.ReadInt16();
                item.Jobs = reader.ReadInt32();
                stream.Position += 0x0D;            // Unknown 18 - 24
                item.CastTime = reader.ReadByte();
                stream.Position += 0x02;            // Unknown 26 - 27
                item.Recast = reader.ReadInt32();
                stream.Position += 0x04;
            }
        }
        static private void ParseArmorItem(Stream stream, ref dynamic item)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Position += 0x0C;            // Unknown 00 - 0B
                item.ValidTargets = (ValidTargets) reader.ReadInt16();
                item.Level = reader.ReadInt16();
                item.Slots = reader.ReadInt16();
                item.Races = reader.ReadInt16();
                item.Jobs = reader.ReadInt32();
                stream.Position += 0x03;            // Unknown 18 - 1A
                item.CastTime = reader.ReadByte();
                stream.Position += 0x04;            // Unknown 1C - 1F
                item.Recast = reader.ReadInt32();
                stream.Position += 0x04;
            }
        }
        static private void ParseUsableItem(Stream stream, ref dynamic item)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Position += 0x0C;            // Unknown 00 - 0B
                item.ValidTargets = (ValidTargets) reader.ReadInt16();
                item.CastTime = reader.ReadInt16();
                stream.Position += 0x08;
            }
        }
        [SuppressMessage("Microsoft.Usage", "CA1801")]
        static private void ParseAutomatonItem(Stream stream, ref dynamic item)
        {
            stream.Position += 0x18;
        }
        [SuppressMessage("Microsoft.Usage", "CA1801")]
        static private void ParseMazeItem(Stream stream, ref dynamic item)
        {
            stream.Position += 0x54;
        }
        static private void ParseMonstrosityItem(Stream stream, ref dynamic item)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                item.TPMoves = new Dictionary<short, byte>();
                stream.Position += 0x30;            // Unknown 00 - 2F
                for (int i = 0x00; i < 0x10; i++)
                {
                    var move = reader.ReadInt16();
                    var level = reader.ReadByte();
                    if (level != 0 && level != 0xFF)
                    {
                        item.TPMoves.Add(move, level);
                    }
                    ++stream.Position;
                }
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
        static private void ParseItemString(Stream stream, ref dynamic item)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                switch (reader.ReadInt32())
                {
                // Japanese
                case 2:
                    item.Japanese = DecodeEntry(stream, StringIndex.Name);
                    item.JapaneseLog = item.Japanese;
                    break;

                // English
                case 5:
                    item.English = DecodeEntry(stream, StringIndex.Name);
                    item.EnglishLog = DecodeEntry(stream, StringIndex.EnglishLogSingular);
                    break;

                // French
                case 6:
                    item.French = DecodeEntry(stream, StringIndex.Name);
                    item.FrenchLog = DecodeEntry(stream, StringIndex.FrenchLogSingular);
                    break;

                // German
                case 9:
                    item.German = DecodeEntry(stream, StringIndex.Name);
                    item.GermanLog = DecodeEntry(stream, StringIndex.GermanLogSingular);
                    break;
                }
            }
        }
        static private object DecodeEntry(Stream stream, StringIndex index)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                long origin = stream.Position;
                stream.Position += 8 * (int) index;
                int dataoffset = reader.ReadInt32();
                int datatype = reader.ReadInt32();

                stream.Position = origin + dataoffset;

                switch (datatype)
                {
                case 0:
                    stream.Position += 0x1C;
                    long dataorigin = stream.Position;
                    int length;

                    try
                    {
                        while (reader.ReadByte() != 0) ;
                    }
                    catch (EndOfStreamException)
                    {
                    }
                    finally
                    {
                        length = (int) (stream.Position - dataorigin);
                        stream.Position = dataorigin;
                    }

                    return FF11ShiftJISDecoder.Decode(reader.ReadBytes(length), 0, length);

                case 1:
                    return reader.ReadInt32();
                }

                return null;
            }
        }
    }
}
