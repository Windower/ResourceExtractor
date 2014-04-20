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

                ability.ID = data[0] | data[1] << 8;
                ability.Type = (AbilityType) data[2];
                ability.Prefix = ((AbilityType) ability.Type).Prefix();
                ability.MPCost = data[6] | data[7] << 8;
                ability.RecastID = data[8] | data[9] << 8;
                ability.Targets = (ValidTargets) (data[10] | data[11] << 8);
                ability.TPCost = data[12] == 0xFF ? 0 : data[12];
                ability.MonsterLevel = data[15];
                ability.Skill = Skill.None;
                ability.Element = Element.None;

                ability.Valid = true;

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

                spell.RecastID = BitConverter.ToInt16(data, 0);
                spell.Type = (MagicType) BitConverter.ToInt16(data, 2);
                spell.Prefix = ((MagicType) spell.Type).Prefix();
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
                using (BinaryReader reader = new BinaryReader(stringstream))
                {
                    item.ID = reader.ReadUInt16();
                    item.Category = "General";

                    if (item.ID >= 0x0001 && item.ID <= 0x0FFF || item.ID >= 0x2200 && item.ID < 0x2800)
                    {
                        ParseGeneralItem(reader, item);
                    }
                    else if (item.ID >= 0x1000 && item.ID < 0x2000)
                    {
                        ParseUsableItem(reader, item);
                    }
                    else if (item.ID >= 0x2000 && item.ID < 0x2200)
                    {
                        ParseAutomatonItem(reader, item);
                    }
                    else if ((item.ID >= 0x2800 && item.ID < 0x4000) || (item.ID >= 0x6400 && item.ID < 0x7000))
                    {
                        ParseArmorItem(reader, item);
                        item.Category = "Armor";
                    }
                    else if (item.ID >= 0x4000 && item.ID < 0x5400)
                    {
                        ParseWeaponItem(reader, item);
                        item.Category = "Weapon";
                    }
                    else if (item.ID >= 0x7000 && item.ID < 0x7400)
                    {
                        ParseMazeItem(reader, item);
                    }
                    //else if (item.ID >= 0xF000 && item.ID < 0xF200)
                    //{
                    //    ParseMonstrosityItem(reader, item);
                    //}
                    else if (item.ID == 0xFFFF)
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
                    else
                    {
                        Console.WriteLine(String.Format("Unknown item ({0})", item.ID));
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
            item.ValidTargets = (ValidTargets) reader.ReadInt16();

            reader.ReadBytes(0x0A);             // Unknown 0E - 17
        }
        static private void ParseWeaponItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.ValidTargets = (ValidTargets) reader.ReadUInt16();
            item.Level = reader.ReadUInt16();
            item.Slots = reader.ReadUInt16();
            item.Races = reader.ReadUInt16();
            item.Jobs = reader.ReadUInt32();
            reader.ReadBytes(0x0D);             // Unknown 18 - 24
            item.CastTime = reader.ReadByte();
            reader.ReadBytes(0x02);             // Unknown 26 - 27
            item.Recast = reader.ReadUInt32();
            reader.ReadBytes(0x04);             // Unknown 2C - 2F
        }
        static private void ParseArmorItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.ValidTargets = (ValidTargets) reader.ReadUInt16();
            item.Level = reader.ReadUInt16();
            item.Slots = reader.ReadUInt16();
            item.Races = reader.ReadUInt16();
            item.Jobs = reader.ReadUInt32();
            reader.ReadBytes(0x03);             // Unknown 18 - 1A
            item.CastTime = reader.ReadByte();
            reader.ReadBytes(0x04);             // Unknown 1C - 1F
            item.Recast = reader.ReadUInt32();
            reader.ReadBytes(0x04);             // Unknown 24 - 27
        }
        static private void ParseUsableItem(BinaryReader reader, dynamic item)
        {
            reader.ReadBytes(0x0A);             // Unknown 02 - 0B
            item.ValidTargets = (ValidTargets) reader.ReadUInt16();
            item.CastTime = reader.ReadUInt16();
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
            item.TPMoves = new Dictionary<ushort, sbyte>();
            reader.ReadBytes(0x2E);             // Unknown 02 - 2F
            for (var i = 0x00; i < 0x10; ++i)
            {
                var move = reader.ReadUInt16();
                var level = reader.ReadSByte();
                if (level != 0 && level != -1 && !item.TPMoves.ContainsKey(move))
                {
                    item.TPMoves.Add(move, level);
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
                item.Japanese = DecodeEntry(reader, StringIndex.Name);
                item.JapaneseLog = item.Japanese;
                break;

            // English
            case 5:
                item.English = DecodeEntry(reader, StringIndex.Name);
                item.EnglishLog = DecodeEntry(reader, StringIndex.EnglishLogSingular);
                break;

            // French
            case 6:
                item.French = DecodeEntry(reader, StringIndex.Name);
                item.FrenchLog = DecodeEntry(reader, StringIndex.FrenchLogSingular);
                break;

            // German
            case 9:
                item.German = DecodeEntry(reader, StringIndex.Name);
                item.GermanLog = DecodeEntry(reader, StringIndex.GermanLogSingular);
                break;

            // Shouldn't happen, suggests new format (or new language)
            default:
                Stream stream = reader.BaseStream;
                stream.Position = 0;
                Console.WriteLine(String.Format("Unknown language format. Item: {0}, Language (#fields): {1}, Data:\n{2}", item.ID, language, BitConverter.ToString(reader.ReadBytes((int) stream.Length)).Replace("-", " ")));
                break;
            }
        }
        static private object DecodeEntry(BinaryReader reader, StringIndex index)
        {
            Stream stream = reader.BaseStream;
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

                while (stream.Position != stream.Length && reader.ReadByte() != 0) ;
                length = (int) (stream.Position - dataorigin) - 1;
                stream.Position = dataorigin;

                return FF11ShiftJISDecoder.Decode(reader.ReadBytes(length), 0, length);

            case 1:
                return reader.ReadInt32();
            }

            return null;
        }
    }
}
