// <copyright file="Program.cs" company="Windower Team">
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

namespace ResourceExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using Microsoft.Win32;
    using ResourceExtractor.Formats;
    using ResourceExtractor.Formats.Items;
    using System.Text;

    internal class Program
    {
        private static void Main()
        {
#if !DEBUG
            try
            {
#endif
                Console.CursorVisible = false;

                string basedirectory = GetBaseDirectory();
                if (basedirectory != null)
                {
                    Directory.CreateDirectory("resources");

                    ExtractItems(basedirectory);

                    IList<object> data = LoadSpellAbilityData(basedirectory);
                    ExtractSpells(basedirectory, data);
                    ExtractAbilities(basedirectory, data);
                    ExtractAreas(basedirectory);
                    ExtractStatuses(basedirectory);

                    ApplyFixes();

                    Console.WriteLine("\nResource extraction complete!");
                }
                else
                {
                    Console.WriteLine("\nUnable to locate Final Fantasy XI installation.");
                }
#if !DEBUG
            }
            catch
            {
                if (Debugger.IsAttached)
                {
                    throw;
                }
            }
#endif

            Console.Write("\nPress any key to exit. ");
            Console.CursorVisible = true;
            Console.ReadKey(true);
        }

        /// Start Lua Code
        private static string Targ_string(ValidTargets Targs)
        {
            string ret = String.Empty;
            foreach (ValidTargets i in Targs.ToList())
            {
                ret += "\"" + i.ToString() + "\",";
            }
            return ret;
        }
        /// End Lua Code

        private static string GetBaseDirectory()
        {
            string basedirectory = null;

            DisplayMessage("Locating Final Fantasy XI installation directory...");

            try
            {
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    RegistryKey key = null;
                    try
                    {
                        key = hklm.OpenSubKey("SOFTWARE\\PlayOnlineUS\\InstallFolder");

                        if (key == null)
                        {
                            key = hklm.OpenSubKey("SOFTWARE\\PlayOnline\\InstallFolder");
                        }

                        if (key == null)
                        {
                            key = hklm.OpenSubKey("SOFTWARE\\PlayOnlineEU\\InstallFolder");
                        }

                        if (key != null)
                        {
                            basedirectory = key.GetValue("0001") as string;
                        }
                    }
                    finally
                    {
                        if (key != null)
                        {
                            key.Dispose();
                        }
                    }
                }
            }
            finally
            {
                if (basedirectory == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return basedirectory;
        }

        private static void ExtractItems(string basedirectory)
        {
            IList<IDictionary<int, Item>> items = LoadItems(basedirectory);
            if (items == null)
            {
                DisplayMessage("\nA problem occurred while loading item data.");
                return;
            }

            DisplayMessage("Generating xml files...");
#if !DEBUG
            try
            {
#endif
                XDocument armor = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("items"));
                XDocument general = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("items"));
                XDocument weapons = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("items"));
                /// Start Lua Code
                using (System.IO.StreamWriter lua_items = new System.IO.StreamWriter(@"C:\lua_items.lua"))
                {
                    lua_items.WriteLine("items = {}");
                    /// End Lua Code

                    string[] ignore = { "." };

                    foreach (int id in items[0].Keys)
                    {
                        Item item = items[0][id];
                        string name = item.Name;
                        /// Start Lua Code
                        string category = "General";
                        /// End Lua Code

                        if (IsValidName(ignore, name))
                        {
                            Item jp = items[1][id];
                            Item de = items[2][id];
                            Item fr = items[3][id];

                            EquippableItem equip = item as EquippableItem;
                            if (equip != null)
                            {
                                XElement root;
                                if (item is WeaponItem)
                                {
                                    /// Start Lua Code
                                    category = "Weapons";
                                    /// End Lua Code
                                    root = weapons.Root;
                                }
                                else
                                {
                                    /// Start Lua Code
                                    category = "Armor";
                                    /// End Lua Code
                                    root = armor.Root;
                                }

                                root.Add(new XElement("i",
                                    new XAttribute("id", id),
                                    new XAttribute("enl", item.LogName),
                                    new XAttribute("fr", fr.Name),
                                    new XAttribute("frl", fr.LogName),
                                    new XAttribute("de", de.Name),
                                    new XAttribute("del", de.LogName),
                                    new XAttribute("jp", jp.Name),
                                    new XAttribute("jpl", string.Empty),
                                    new XAttribute("slots", String.Format(CultureInfo.InvariantCulture, "{0:X4}", equip.Slots)),
                                    new XAttribute("jobs", String.Format(CultureInfo.InvariantCulture, "{0:X8}", equip.Jobs)),
                                    new XAttribute("races", String.Format(CultureInfo.InvariantCulture, "{0:X4}", equip.Races)),
                                    new XAttribute("level", equip.Level),
                                    new XAttribute("targets", equip.ValidTargets),
                                    new XAttribute("casttime", equip.CastTime),
                                    new XAttribute("recast", equip.Recast),
                                    name));

                                /// Start Lua Code
                                lua_items.WriteLine("items[{0}] = {{ id={0},english=\"{1}\",english_log=\"{2}\",french=\"{3}\",french_log=\"{4}\",german=\"{5}\",german_log=\"{6}\",japanese=\"{7}\",japanese_log=\"\",targets=S({8}),cast_time={9},recast={10},category=\"{11}\",level={12},slots=resources.parse_flags(0x{13}),jobs=resources.parse_flags(0x{14}),races=resources.parse_flags(0x{15}) }}", id, name, item.LogName.Trim(), fr.Name, fr.LogName.Trim(), de.Name.Trim(), de.LogName.Trim(), jp.Name.Trim(), Targ_string(equip.ValidTargets), equip.CastTime, equip.Recast, category, equip.Level, String.Format(CultureInfo.InvariantCulture, "{0:X4}", equip.Slots), String.Format(CultureInfo.InvariantCulture, "{0:X8}", equip.Jobs), String.Format(CultureInfo.InvariantCulture, "{0:X4}", equip.Races));
                                /// End Lua Code
                            }
                            else
                            {
                                XElement element = new XElement("i",
                                        new XAttribute("id", id),
                                        new XAttribute("enl", item.LogName),
                                        new XAttribute("fr", fr.Name),
                                        new XAttribute("frl", fr.LogName),
                                        new XAttribute("de", de.Name),
                                        new XAttribute("del", de.LogName),
                                        new XAttribute("jp", jp.Name),
                                        new XAttribute("jpl", string.Empty),
                                        name);
                                
                                GeneralItem generalitem = item as GeneralItem;
                                if (generalitem != null)
                                {
                                    element.Add(new XAttribute("targets", generalitem.ValidTargets));
                                    /// Start Lua Code
                                    lua_items.WriteLine("items[{0}] = {{ id={0},english=\"{1}\",english_log=\"{2}\",french=\"{3}\",french_log=\"{4}\",german=\"{5}\",german_log=\"{6}\",japanese=\"{7}\",japanese_log=\"\",targets=S({8}),cast_time=0,category=\"{9}\" }}", id, name, item.LogName.Trim(), fr.Name, fr.LogName.Trim(), de.Name.Trim(), de.LogName.Trim(), jp.Name.Trim(), Targ_string(generalitem.ValidTargets), category);
                                    /// End Lua Code
                                }

                                UsableItem usableitem = item as UsableItem;
                                if (usableitem != null)
                                {
                                    element.Add(new XAttribute("targets", usableitem.ValidTargets));
                                    element.Add(new XAttribute("casttime", usableitem.CastTime));
                                    /// Start Lua Code
                                    lua_items.WriteLine("items[{0}] = {{ id={0},english=\"{1}\",english_log=\"{2}\",french=\"{3}\",french_log=\"{4}\",german=\"{5}\",german_log=\"{6}\",japanese=\"{7}\",japanese_log=\"\",targets=S({8}),cast_time={9},category=\"{10}\" }}", id, name, item.LogName.Trim(), fr.Name, fr.LogName.Trim(), de.Name.Trim(), de.LogName.Trim(), jp.Name.Trim(), Targ_string(usableitem.ValidTargets), usableitem.CastTime, category);
                                    /// End Lua Code
                                }

                                /// Start Lua Code
                                MazeItem mazeitem = item as MazeItem;
                                BasicItem basicitem = item as BasicItem;
                                AutomatonItem automatonitem = item as AutomatonItem;
                                if (mazeitem != null | basicitem != null | automatonitem != null)
                                {
                                    lua_items.WriteLine("items[{0}] = {{ id={0},english=\"{1}\",english_log=\"{2}\",french=\"{3}\",french_log=\"{4}\",german=\"{5}\",german_log=\"{6}\",japanese=\"{7}\",japanese_log=\"\",category=\"{8}\" }}", id, name, item.LogName.Trim(), fr.Name, fr.LogName.Trim(), de.Name.Trim(), de.LogName.Trim(), jp.Name.Trim(), category);
                                }
                                /// End Lua Code

                                general.Root.Add(element);
                            }
                        }
                    }
                /// Start Lua Code
                }
                /// End Lua Code

                armor.Root.ReplaceNodes(armor.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));
                general.Root.ReplaceNodes(general.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));
                weapons.Root.ReplaceNodes(weapons.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                armor.Save(Path.Combine("resources", "items_armor.xml"));
                general.Save(Path.Combine("resources", "items_general.xml"));
                weapons.Save(Path.Combine("resources", "items_weapons.xml"));
#if !DEBUG
            }
            catch
            {
                DisplayResult("Error", ConsoleColor.DarkRed);
                throw;
            }
#endif

            DisplayResult("Done!", ConsoleColor.DarkGreen);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static void ExtractSpells(string basedirectory, IList<object> datalist)
        {
            IList<SpellData> data = null;
            foreach (object o in datalist)
            {
                data = o as IList<SpellData>;
                if (data != null)
                {
                    break;
                }
            }

            if (data == null)
            {
                DisplayMessage("\nUnable to find spell data.");
                return;
            }

            IList<IList<IList<string>>> names = LoadSpellNames(basedirectory);
            if (names == null)
            {
                DisplayMessage("\nA problem occurred while loading spell names.");
                return;
            }

            if (names == null)
            {
                return;
            }

#if !DEBUG
            try
            {
#endif
                DisplayMessage("Generating xml file...");

                XDocument spells = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("spells"));
                /// Start Lua Code
                using (System.IO.StreamWriter lua_spells = new System.IO.StreamWriter(@"C:\lua_spells.lua"))
                {
                    lua_spells.WriteLine("spells = {}");
                    /// End Lua Code

                    string[] ignore = { "." };

                    foreach (SpellData spell in data)
                    {
                        if (!spell.Valid)
                        {
                            continue;
                        }

                        int id = spell.Index;
                        string en = names[0][id][0];
                        string jp = names[1][id][0];
                        string de = names[2][id][0];
                        string fr = names[3][id][0];

                        if (IsValidName(ignore, en, de, fr, jp))
                        {
                            string prefix = "/unknown";
                            switch (spell.MagicType)
                            {
                                case MagicType.WhiteMagic:
                                case MagicType.BlackMagic:
                                case MagicType.SummonerPact:
                                case MagicType.BlueMagic:
                                case MagicType.Geomancy:
                                case MagicType.Trust:
                                    prefix = "/magic";
                                    break;

                                case MagicType.BardSong:
                                    prefix = "/song";
                                    break;

                                case MagicType.Ninjutsu:
                                    prefix = "/ninjutsu";
                                    break;
                            }

                            Element element = spell.Element;
                            switch (spell.IconId)
                            {
                                case 56: element = Element.Fire; break;
                                case 57: element = Element.Ice; break;
                                case 58: element = Element.Wind; break;
                                case 59: element = Element.Earth; break;
                                case 60: element = Element.Thunder; break;
                                case 61: element = Element.Water; break;
                                case 62: element = Element.Light; break;
                                case 63: element = Element.Dark; break;
                                case 64: element = Element.None; break;
                            }

                            spells.Root.Add(new XElement("s",
                                new XAttribute("id", spell.Id),
                                new XAttribute("index", spell.Index),
                                new XAttribute("prefix", prefix),
                                new XAttribute("english", en),
                                new XAttribute("german", de),
                                new XAttribute("french", fr),
                                new XAttribute("japanese", jp),
                                new XAttribute("type", spell.MagicType),
                                new XAttribute("element", element),
                                new XAttribute("targets", spell.ValidTargets),
                                new XAttribute("skill", spell.Skill),
                                new XAttribute("mpcost", spell.MPCost),
                                new XAttribute("casttime", spell.CastTime),
                                new XAttribute("recast", spell.Recast),
                                new XAttribute("alias", string.Empty)));
                            /// Start Lua Code
                            lua_spells.WriteLine("spells[{1}] = {{ id={0},index={1},prefix=\"{2}\",english=\"{3}\",french=\"{4}\",german=\"{5}\",japanese=\"{6}\",type=\"{7}\",element=\"{8}\",targets=S({9}),skill=\"{10}\",mp_cost=\"{11}\",cast_time={12},recast={13},alias=\"{14}\" }}", spell.Id, spell.Index, prefix, en, fr, de, jp, spell.MagicType.ToString(), element, Targ_string(spell.ValidTargets), spell.Skill, spell.MPCost, spell.CastTime, spell.Recast, string.Empty);
                            /// End Lua Code
                        }
                    }
                /// Start Lua Code
                }
                /// End Lua Code

                spells.Root.ReplaceNodes(spells.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                spells.Save(Path.Combine("resources", "spells.xml"));
#if !DEBUG
            }
            catch
            {
                DisplayResult("Error", ConsoleColor.DarkRed);
                throw;
            }
#endif

            DisplayResult("Done!", ConsoleColor.DarkGreen);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static void ExtractAbilities(string basedirectory, IList<object> datalist)
        {
            IList<AbilityData> data = null;
            foreach (object o in datalist)
            {
                data = o as IList<AbilityData>;
                if (data != null)
                {
                    break;
                }
            }

            if (data == null)
            {
                DisplayMessage("\nUnable to find ability data.");
                return;
            }

            IList<IList<IList<string>>> names = LoadAbilityNames(basedirectory);
            if (names == null)
            {
                DisplayMessage("\nA problem occurred while loading ability names.");
                return;
            }

#if !DEBUG
            try
            {
#endif
                DisplayMessage("Generating xml file...");

                XDocument abilities = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("abils"));
                /// Start Lua Code
                using (System.IO.StreamWriter lua_abilities = new System.IO.StreamWriter(@"C:\lua_abilities.lua"))
                {
                    lua_abilities.WriteLine("abilities = {}");
                    /// End Lua Code

                    string[] ignore = { "." };

                    foreach (AbilityData ability in data)
                    {
                        int id = ability.Id;
                        string en = names[0][id][0];
                        string jp = names[1][id][0];
                        string de = names[2][id][0];
                        string fr = names[3][id][0];

                        if (IsValidName(ignore, en, de, fr, jp) && !en.StartsWith("#", StringComparison.Ordinal))
                        {
                            string prefix = "/unknown";
                            switch (ability.AbilityType)
                            {
                                case AbilityType.Misc:
                                case AbilityType.JobTrait:
                                    prefix = "/echo";
                                    break;

                                case AbilityType.JobAbility:
                                case AbilityType.CorsairRoll:
                                case AbilityType.CorsairShot:
                                case AbilityType.Samba:
                                case AbilityType.Waltz:
                                case AbilityType.Step:
                                case AbilityType.Jig:
                                case AbilityType.Flourish1:
                                case AbilityType.Flourish2:
                                case AbilityType.Flourish3:
                                case AbilityType.Scholar:
                                case AbilityType.Rune:
                                case AbilityType.Ward:
                                case AbilityType.Effusion:
                                    prefix = "/jobability";
                                    break;

                                case AbilityType.WeaponSkill:
                                    prefix = "/weaponskill";
                                    break;

                                case AbilityType.MonsterSkill:
                                    prefix = "/monsterskill";
                                    break;

                                case AbilityType.PetCommand:
                                case AbilityType.BloodPactWard:
                                case AbilityType.BloodPactRage:
                                case AbilityType.Monster:
                                    prefix = "/pet";
                                    break;
                            }

                            abilities.Root.Add(new XElement("a",
                                new XAttribute("id", id),
                                new XAttribute("index", ability.TimerId),
                                new XAttribute("prefix", prefix),
                                new XAttribute("english", en),
                                new XAttribute("german", de),
                                new XAttribute("french", fr),
                                new XAttribute("japanese", jp),
                                new XAttribute("type", ability.AbilityType),
                                new XAttribute("element", Element.None),
                                new XAttribute("targets", ability.ValidTargets),
                                new XAttribute("skill", "Ability"),
                                new XAttribute("mpcost", ability.MPCost),
                                new XAttribute("tpcost", ability.TPCost),
                                new XAttribute("casttime", 0),
                                new XAttribute("recast", 0),
                                new XAttribute("alias", string.Empty)));
                            /// Start Lua Code
                            lua_abilities.WriteLine("abilities[{0}] = {{ id={0},index={1},prefix=\"{2}\",english=\"{3}\",french=\"{4}\",german=\"{5}\",japanese=\"{6}\",type=\"{7}\",element=\"{8}\",targets=S({9}),skill=\"Ability\",mp_cost=\"{10}\",tp_cost=\"{11}\",cast_time=0,recast=0,alias=\"{12}\" }}", ability.Id, ability.TimerId, prefix, en, fr, de, jp, ability.AbilityType.ToString(), Element.None, Targ_string(ability.ValidTargets), "Ability", ability.MPCost, ability.TPCost, string.Empty);
                            /// End Lua Code
                        }
                    }
                /// Start Lua Code
                }
                /// End Lua Code

                abilities.Root.ReplaceNodes(abilities.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                abilities.Save(Path.Combine("resources", "abils.xml"));
#if !DEBUG
            }
            catch
            {
                DisplayResult("Error", ConsoleColor.DarkRed);
                throw;
            }
#endif

            DisplayResult("Done!", ConsoleColor.DarkGreen);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static void ExtractAreas(string basedirectory)
        {
            IList<IList<IList<string>>> names = LoadAreaNames(basedirectory);
            if (names == null)
            {
                DisplayMessage("\nA problem occurred while loading area names.");
                return;
            }

#if !DEBUG
            try
            {
#endif
                DisplayMessage("Generating xml file...");

                XDocument areas = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("areas"));

                int count = names[0].Count;

                string[] ignore = { "none" };

                for (int id = 0; id < count; id++)
                {
                    string en = names[0][id][0];
                    string jp = names[1][id][0];
                    string de = names[2][id][0];
                    string fr = names[3][id][0];

                    if (IsValidName(ignore, en, de, fr, jp))
                    {
                        areas.Root.Add(new XElement("a",
                            new XAttribute("id", id),
                            new XAttribute("fr", fr),
                            new XAttribute("de", de),
                            new XAttribute("jp", jp),
                            en));
                    }
                }

                areas.Root.ReplaceNodes(areas.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                areas.Save(Path.Combine("resources", "areas.xml"));
#if !DEBUG
            }
            catch
            {
                DisplayResult("Error", ConsoleColor.DarkRed);
                throw;
            }
#endif

            DisplayResult("Done!", ConsoleColor.DarkGreen);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static void ExtractStatuses(string basedirectory)
        {
            IList<IList<IList<string>>> names = LoadStatusNames(basedirectory);
            if (names == null)
            {
                DisplayMessage("\nA problem occurred while loading status names.");
                return;
            }

#if !DEBUG
            try
            {
#endif
                DisplayMessage("Generating xml file...");

                XDocument statuses = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("status"));

                int count = names[0].Count;

                string[] ignore = { ".", "(None)", "(Imagery)" };

                for (int id = 0; id < count; id++)
                {
                    string en = names[0][id][0];
                    string jp = names[1][id][0];
                    string de = names[2][id][1];
                    string fr = names[3][id][2];
                    string enl = names[0][id][1];

                    if (IsValidName(ignore, en, de, fr, jp))
                    {
                        statuses.Root.Add(new XElement("b",
                            new XAttribute("id", id),
                            new XAttribute("duration", 0),
                            new XAttribute("fr", fr),
                            new XAttribute("de", de),
                            new XAttribute("jp", jp),
                            new XAttribute("enLog", enl),
                            en));
                    }
                }

                statuses.Root.ReplaceNodes(statuses.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                statuses.Save(Path.Combine("resources", "status.xml"));
#if !DEBUG
            }
            catch
            {
                DisplayResult("Error", ConsoleColor.DarkRed);
                throw;
            }
#endif

            DisplayResult("Done!", ConsoleColor.DarkGreen);
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static void ApplyFixes()
        {
            DisplayMessage("Applying fixes...");
#if !DEBUG
            try
            {
#endif
                XDocument fixes = XDocument.Load("fixes.xml");

                foreach (XElement fixset in fixes.Root.Elements())
                {
                    string path = Path.Combine("resources", string.Format(CultureInfo.InvariantCulture, "{0}.xml", fixset.Name.LocalName));
                    XDocument list = XDocument.Load(path);

                    string key = (string)fixset.Attribute("key") ?? "id";

                    XElement update = fixset.Element("update");
                    if (update != null)
                    {
                        foreach (XElement fix in update.Elements())
                        {
                            IEnumerable<XElement> elements = from e in list.Root.Elements(fix.Name.LocalName)
                                                             where (string)e.Attribute(key) == (string)fix.Attribute(key)
                                                             select e;

                            if (elements.Count() <= 0)
                            {
                                list.Root.Add(fix);
                            }
                            else
                            {
                                foreach (XAttribute attr in fix.Attributes())
                                {
                                    string name = attr.Name.LocalName;
                                    if (name == key)
                                    {
                                        continue;
                                    }

                                    foreach (XElement e in elements)
                                    {
                                        XAttribute a = e.Attribute(name);
                                        if (a == null)
                                        {
                                            e.Add(new XAttribute(attr));
                                        }
                                        else
                                        {
                                            a.Value = attr.Value;
                                        }
                                    }
                                }

                                if (!fix.IsEmpty)
                                {
                                    foreach (XElement e in elements)
                                    {
                                        e.Value = fix.Value;
                                    }
                                }
                            }
                        }
                    }

                    XElement remove = fixset.Element("remove");
                    if (remove != null)
                    {
                        foreach (XElement fix in remove.Elements())
                        {
                            IEnumerable<XElement> elements = from e in list.Root.Elements(fix.Name.LocalName)
                                                             where (string)e.Attribute(key) == (string)fix.Attribute(key)
                                                             select e;

                            foreach (XElement e in elements)
                            {
                                e.Remove();
                            }
                        }
                    }

                    list.Root.ReplaceNodes(list.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute(key) ?? 0)));

                    list.Save(path);
                }
#if !DEBUG
            }
            catch
            {
                DisplayResult("Error", ConsoleColor.DarkRed);
                throw;
            }
#endif

                DisplayResult("Done!", ConsoleColor.DarkGreen);
        }

        private static IList<IDictionary<int, Item>> LoadItems(string basedirectory)
        {
            IList<IDictionary<int, Item>> result = null;

            try
            {
                DisplayMessage("Loading item data...");

                int[][] fileids =
                    {
                        new int[] { 0x0049, 0x004A, 0x004D, 0x004C, 0x004B, 0x005B, 0xD973, 0xD974, 0xD977 },
                        new int[] { 0x0004, 0x0005, 0x0008, 0x0007, 0x0006, 0x0009, 0xD8FB, 0xD8FC, 0xD8FF },
                        new int[] { 0xDA07, 0xDA08, 0xDA0B, 0xDA0A, 0xDA09, 0xD9EB, 0xD9EC, 0xD9EF, 0xDA0C },
                        new int[] { 0xDBAB, 0xDBAC, 0xDBAF, 0xDBAE, 0xDBAD, 0xDB8F, 0xDB90, 0xDB93, 0xDBB0 },
                    };

                result = new List<IDictionary<int, Item>>();

                byte[] data = new byte[0x200];
                foreach (int[] ids in fileids)
                {
                    IDictionary<int, Item> items = new Dictionary<int, Item>();
                    result.Add(items);

                    foreach (int id in ids)
                    {
                        string path = GetPath(basedirectory, id);

                        using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            int count = (int)(stream.Length / 0xC00);
                            for (int i = 0; i < count; i++)
                            {
                                stream.Position = i * 0xC00;
                                stream.Read(data, 0, data.Length);
                                Item item = Item.CreateItem(data);
                                if (!items.ContainsKey(item.Id))
                                {
                                    items.Add(item.Id, item);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (result == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return result;
        }

        private static IList<object> LoadSpellAbilityData(string basedirectory)
        {
            IList<object> result = null;

            try
            {
                DisplayMessage("Loading spell and ability data...");

                string path = GetPath(basedirectory, 0x0051);
                using (FileStream stream = File.OpenRead(path))
                {
                    result = new Container(stream);
                }
            }
            finally
            {
                if (result == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return result;
        }

        private static IList<IList<IList<string>>> LoadSpellNames(string basedirectory)
        {
            IList<IList<IList<string>>> result = null;

            try
            {
                DisplayMessage("Loading spell names...");

                int[] fileids = new int[] { 0xD996, 0xD91E, 0xDA0E, 0xDBB2 };

                result = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(basedirectory, id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        result.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (result == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return result;
        }

        private static IList<IList<IList<string>>> LoadAbilityNames(string basedirectory)
        {
            IList<IList<IList<string>>> result = null;

            try
            {
                DisplayMessage("Loading ability names...");

                int[] fileids = new int[] { 0xD995, 0xD91D, 0xDA0D, 0xDBB1 };

                result = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(basedirectory, id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        result.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (result == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return result;
        }

        private static IList<IList<IList<string>>> LoadAreaNames(string basedirectory)
        {
            IList<IList<IList<string>>> result = null;

            try
            {
                DisplayMessage("Loading area names...");

                int[] fileids = new int[] { 0xD8A9, 0xD8EF, 0xD9DF, 0xDB83 };

                result = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(basedirectory, id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        result.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (result == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return result;
        }

        private static IList<IList<IList<string>>> LoadStatusNames(string basedirectory)
        {
            IList<IList<IList<string>>> result = null;

            try
            {
                DisplayMessage("Loading status names...");

                int[] fileids = new int[] { 0xD9AD, 0xD935, 0xDA2C, 0xDBD0 };

                result = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(basedirectory, id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        result.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (result == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return result;
        }

        private static bool IsValidName(string[] ignore, params string[] names)
        {
            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name) || ignore.Contains(name))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetPath(string basedirectory, int id)
        {
            string ftable = Path.Combine(basedirectory, "FTABLE.DAT");

            using (FileStream fstream = File.OpenRead(ftable))
            {
                fstream.Position = id * 2;
                int file = fstream.ReadByte() | fstream.ReadByte() << 8;
                return Path.Combine(
                    basedirectory, "ROM",
                    string.Format(CultureInfo.InvariantCulture, "{0}", file >> 7),
                    string.Format(CultureInfo.InvariantCulture, "{0}.DAT", file & 0x7F));
            }
        }

        private static void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        private static void DisplayResult(string result, ConsoleColor color)
        {
            Console.CursorTop = Console.CursorTop - 1;
            Console.CursorLeft = Console.BufferWidth - result.Length - 2;

            ConsoleColor currentcolor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write("[{0}]", result);
            }
            finally
            {
                Console.ForegroundColor = currentcolor;
            }
        }
    }
}
