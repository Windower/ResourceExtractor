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
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using System.Text;
    using Microsoft.Win32;
    using ResourceExtractor.Serializers.Lua;

    internal class Program
    {
        private static string Dir { get; set; }
        private static dynamic Data;
        private static void Main()
        {
#if !DEBUG
            try
            {
#endif
            Console.CursorVisible = false;

            Data = new ExpandoObject();
            Data.abilities = new List<dynamic>();
            Data.spells = new List<dynamic>();
            Data.zones = new List<dynamic>();
            Data.buffs = new List<dynamic>();
            Data.items = new List<dynamic>();

            Dir = GetBaseDirectory();
            if (Dir != null)
            {
                Directory.CreateDirectory("resources");
                Directory.CreateDirectory("resources/lua");
                Directory.CreateDirectory("resources/xml");
                Directory.CreateDirectory("resources/json");

                LoadSpellAbilityData();
                LoadBuffData();
                LoadZoneData();
                LoadItemData();
                
                ApplyFixes();

                Extract("items", new string[] { "." });
                Extract("abilities", new string[] { "." });
                Extract("spells", new string[] { "." });
                Extract("buffs", new string[] { ".", "(None)", "(Imagery)" });
                Extract("zones", new string[] { "none" });

                Console.WriteLine("Resource extraction complete!");
            }
            else
            {
                Console.WriteLine("Unable to locate Final Fantasy XI installation.");
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

            Console.Write("Press any key to exit. ");
            Console.CursorVisible = true;
            Console.ReadKey(true);
        }

        private static string GetBaseDirectory()
        {
            Dir = null;

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
                            Dir = key.GetValue("0001") as string;
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
                if (Dir == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            return Dir;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static void Extract(string name, string[] ignore)
        {
            DisplayMessage("Generating files for " + name + "...");

#if !DEBUG
            try
            {
#endif
            XDocument xml = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(name));
            LuaFile lua = new LuaFile(name);

            foreach (dynamic obj in ((IDictionary<string, dynamic>) Data)[name])
            {
                if (IsValidName(ignore, obj))
                {
                    XElement xmlelement = new XElement("o");
                    foreach (KeyValuePair<string, object> pair in obj)
                    {
                        xmlelement.SetAttributeValue(pair.Key, pair.Value);
                    }

                    xml.Root.Add(xmlelement);
                    lua.Add(obj);
                }
            }

            xml.Root.ReplaceNodes(xml.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

            xml.Save(Path.Combine("resources", "xml", String.Format("{0}.xml", name)));
            lua.Save();

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
                if (fixset.Name.LocalName == "timers")
                {
                    continue;
                }
                IList<dynamic> data = (IList<dynamic>) ((IDictionary<string, object>) Data)[fixset.Name.LocalName];

                XElement update = fixset.Element("update");
                if (update != null)
                {
                    foreach (XElement fix in update.Elements())
                    {
                        var elements = from e in data
                                       where e.id == Convert.ToInt32(fix.Attribute("id").Value)
                                       select e;

                        if (!elements.Any())
                        {
                            dynamic el = new ExpandoObject();
                            IDictionary<string, object> del = (IDictionary<string, object>) el;
                            foreach (XAttribute attr in fix.Attributes())
                            {
                                del[attr.Name.LocalName] = attr.Parse();
                            }
                            data.Add(el);
                            continue;
                        }
                        else
                        {
                            var element = (IDictionary<string, object>) elements.Single();
                            foreach (XAttribute attr in fix.Attributes())
                            {
                                element[attr.Name.LocalName] = attr.Parse();
                            }
                        }
                    }
                }

                XElement remove = fixset.Element("remove");
                if (remove != null)
                {
                    foreach (XElement fix in remove.Elements())
                    {
                        ((List<dynamic>) data).RemoveAll(x => x.id == Convert.ToInt32(fix.Attribute("id").Value));
                    }
                }
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

        private static void LoadItemData()
        {
            Data.items = new List<dynamic>();

            try
            {
                DisplayMessage("Loading item data...");

                int[][] fileids =
                    {
                        new int[] { 0x0049, 0x004A, 0x004D, 0x004C, 0x004B, 0x005B, 0xD973, 0xD974, 0xD977, 0xD975 },
                        new int[] { 0x0004, 0x0005, 0x0008, 0x0007, 0x0006, 0x0009, 0xD8FB, 0xD8FC, 0xD8FF, 0xD8FD },
                        new int[] { 0xDA07, 0xDA08, 0xDA0B, 0xDA0A, 0xDA09, 0xDA0C, 0xD9EB, 0xD9EC, 0xD9EF, 0xD9ED },
                        new int[] { 0xDBAB, 0xDBAC, 0xDBAF, 0xDBAE, 0xDBAD, 0xDBB0, 0xDB8F, 0xDB90, 0xDB93, 0xDB91 },
                    };

                for (var i = 0; i < fileids[0].Length; ++i)
                {
                    using (
                        FileStream  stream   = File.Open(GetPath(fileids[0][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                                    streamja = File.Open(GetPath(fileids[1][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                                    streamde = File.Open(GetPath(fileids[2][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                                    streamfr = File.Open(GetPath(fileids[3][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        Data.items.AddRange(ResourceParser.ParseItems(stream, streamja, streamde, streamfr));
                    }
                }
            }
            finally
            {
                if (Data.items.Count == 0)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1800")]
        private static void LoadSpellAbilityData()
        {
            // Ability names
            IList<IList<IList<string>>> abilnames = null;
            try
            {
                DisplayMessage("Loading ability names...");

                int[] fileids = new int[] { 0xD995, 0xD91D, 0xDA0D, 0xDBB1 };

                abilnames = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        abilnames.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (abilnames == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            // Spell names
            IList<IList<IList<string>>> spellnames = null;

            try
            {
                DisplayMessage("Loading spell names...");

                int[] fileids = new int[] { 0xD996, 0xD91E, 0xDA0E, 0xDBB2 };

                spellnames = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        spellnames.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (spellnames == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            IList<object> data = null;

            try
            {
                DisplayMessage("Looking up ability and spell data...");

                using (FileStream stream = File.OpenRead(GetPath(0x0051)))
                {
                    data = new Container(stream);
                }
            }
            finally
            {
                if (data == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            DisplayMessage("Loading ability and spell data...");
            foreach (object o in data)
            {
                if (Data.abilities.Count == 0)
                {
                    var kvp = o as KeyValuePair<string, object>?;
                    if (kvp.HasValue && kvp.Value.Key == "abilities")
                    {
                        Data.abilities = kvp.Value.Value;
                    }
                    continue;
                }
                if (Data.spells.Count == 0)
                {
                    var kvp = o as KeyValuePair<string, object>?;
                    if (kvp.HasValue && kvp.Value.Key == "spells")
                    {
                        Data.spells = kvp.Value.Value;
                    }
                    continue;
                }

                if (Data.spells.Count != 0 && Data.abilities.Count != 0)
                {
                    break;
                }
            }

            if (Data.abilities == null)
            {
                DisplayResult("Error!", ConsoleColor.DarkRed);
            }
            else
            {
                DisplayMessage("Adding ability names...");
                foreach (dynamic ability in Data.abilities)
                {
                    ability.en = abilnames[Languages.English][ability.id][0];
                    ability.ja = abilnames[Languages.Japanese][ability.id][0];
                    ability.de = abilnames[Languages.German][ability.id][0];
                    ability.fr = abilnames[Languages.French][ability.id][0];
                }
                DisplayResult("Done!", ConsoleColor.DarkGreen);
            }

            if (Data.spells == null)
            {
                DisplayResult("Error!", ConsoleColor.DarkRed);
            }
            else
            {
                DisplayMessage("Adding spell names...");
                foreach (dynamic spell in Data.spells)
                {
                    spell.en = spellnames[Languages.English][spell.id][0];
                    spell.ja = spellnames[Languages.Japanese][spell.id][0];
                    spell.de = spellnames[Languages.German][spell.id][0];
                    spell.fr = spellnames[Languages.French][spell.id][0];
                }
                DisplayResult("Done!", ConsoleColor.DarkGreen);
            }
        }

        private static void LoadNames(string Name, int[] fileids, int[] indices, int[] logindices = null)
        {
            IList<IList<IList<string>>> names = null;

            try
            {
                DisplayMessage("Loading " + Name + " names...");

                names = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        names.Add(new DMsgStringList(stream));
                    }
                }
            }
            finally
            {
                if (names == null)
                {
                    DisplayResult("Error", ConsoleColor.DarkRed);
                }
                else
                {
                    DisplayResult("Done!", ConsoleColor.DarkGreen);
                }
            }

            if (names == null)
            {
                return;
            }

            var l = (List<dynamic>) ((IDictionary<string, object>) Data)[Name];
            bool add = !l.Any();

            for (int id = 0; id < names[0].Count; ++id)
            {
                dynamic obj;
                if (add)
                {
                    obj = new ExpandoObject();
                    obj.id = id;
                }
                else
                {
                    obj = l[id];
                    if (obj == null)
                    {
                        continue;
                    }
                }

                obj.en = names[Languages.English][id][indices[Languages.English]];
                obj.ja = names[Languages.Japanese][id][indices[Languages.Japanese]];
                obj.de = names[Languages.German][id][indices[Languages.German]];
                obj.fr = names[Languages.French][id][indices[Languages.French]];

                if (logindices != null)
                {
                    obj.enl = names[Languages.English][id][logindices[Languages.English]];
                    obj.jal = names[Languages.Japanese][id][logindices[Languages.Japanese]];
                    obj.del = names[Languages.German][id][logindices[Languages.German]];
                    obj.frl = names[Languages.French][id][logindices[Languages.French]];
                }

                if (add)
                {
                    l.Add(obj);
                }
            }
        }

        private static void LoadBuffData()
        {
            LoadNames("buffs", new int[] { 0xD9AD, 0xD935, 0xDA2C, 0xDBD0 }, new int[] { 0, 0, 1, 2 }, new int[] { 1, 0, 1, 2 });
        }

        private static void LoadZoneData()
        {
            LoadNames("buffs", new int[] { 0xD8A9, 0xD8EF, 0xD9DF, 0xDB83 }, new int[] { 0, 0, 0, 0 });
        }

        private static IList<IList<IList<string>>> LoadMonsterAbilityNames()
        {
            IList<IList<IList<string>>> result = null;

            try
            {
                DisplayMessage("Loading monster ability names...");

                int[] fileids = new int[] { 0x1B7B, 0x1B8C, 0xDA2B, 0xDBCF };

                result = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        // TODO: Format is wrong
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

        private static IList<IList<IList<string>>> LoadActionMessages()
        {
            IList<IList<IList<string>>> result = null;

            try
            {
                DisplayMessage("Loading action messages...");

                int[] fileids = new int[] { 0x1B73, 0x1B72, 0xDA28, 0xDBCC };

                result = new List<IList<IList<string>>>();

                foreach (int id in fileids)
                {
                    string path = GetPath(id);
                    using (FileStream stream = File.OpenRead(path))
                    {
                        // TODO: Format is wrong
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

        private static bool IsValidName(string[] ignore, dynamic res)
        {
            return !(String.IsNullOrWhiteSpace(res.en) || ignore.Contains((string) res.en)
                || String.IsNullOrWhiteSpace(res.ja) || ignore.Contains((string) res.ja)
                || String.IsNullOrWhiteSpace(res.de) || ignore.Contains((string) res.de)
                || String.IsNullOrWhiteSpace(res.fr) || ignore.Contains((string) res.fr)
                || res.en.StartsWith("#", StringComparison.Ordinal));
        }

        private static string GetPath(int id)
        {
            string ftable = Path.Combine(Dir, "FTABLE.DAT");

            using (FileStream fstream = File.OpenRead(ftable))
            {
                fstream.Position = id * 2;
                int file = fstream.ReadByte() | fstream.ReadByte() << 8;
                return Path.Combine(
                    Dir, "ROM",
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
