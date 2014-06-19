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
    using System.Xml.Linq;
    using Microsoft.Win32;
    using ResourceExtractor.Serializers.Lua;

    internal class Program
    {
        private static dynamic model;
        private static string[] categories = new string[] {
            "actions",
            "ability_recasts",
            "buffs",
            "commands",
            "items",
            "job_abilities",
            "job_traits",
            "key_items",
            "monster_abilities",
            "monstrosity",
            "regions",
            "spells",
            "spell_recasts",
            "weapon_skills",
            "zones",
        };
        private static Dictionary<string, Dictionary<ushort, Dictionary<int, string>>> DatLut = new Dictionary<string, Dictionary<ushort, Dictionary<int, string>>> {
            {"actions", new Dictionary<ushort, Dictionary<int, string>> {
                {0xD995, new Dictionary<int, string> {
                    {0, "en"},
                }},
                {0xD91D, new Dictionary<int, string> {
                    {0, "ja"},
                }},
                {0xDA0D, new Dictionary<int, string> {
                    {0, "de"},
                }},
                {0xDBB1, new Dictionary<int, string> {
                    {0, "fr"},
                }},
            }},
            {"buffs", new Dictionary<ushort, Dictionary<int, string>> {
                {0xD9AD, new Dictionary<int, string> {
                    {0, "en"},
                    {1, "enl"},
                }},
                {0xD935, new Dictionary<int, string> {
                    {0, "ja"},
                }},
                {0xDA2C, new Dictionary<int, string> {
                    {1, "de"},
                }},
                {0xDBD0, new Dictionary<int, string> {
                    {2, "fr"},
                }},
            }},
            {"key_items", new Dictionary<ushort, Dictionary<int, string>> {
                {0xD98F, new Dictionary<int, string> {
                    {0, "id"},
                    {4, "en"},
                    //{6, "endesc"},
                }},
                {0xD917, new Dictionary<int, string> {
                    {1, "ja"},
                    //{2, "jadesc"},
                }},
                {0xDA11, new Dictionary<int, string> {
                    {4, "de"},
                }},
                {0xDBB5, new Dictionary<int, string> {
                    {5, "fr"},
                }},
            }},
            {"monster_abilities", new Dictionary<ushort, Dictionary<int, string>> {
                {0x1B7B, new Dictionary<int, string> {
                    {0, "en"},
                }},
                {0x1B7A, new Dictionary<int, string> {
                    {0, "ja"},
                }},
                {0xDA2B, new Dictionary<int, string> {
                    {0, "de"},
                }},
                {0xDBCF, new Dictionary<int, string> {
                    {0, "fr"},
                }},
            }},
            {"regions", new Dictionary<ushort, Dictionary<int, string>> {
                {0xD966, new Dictionary<int, string> {
                    {0, "en"},
                }},
                {0xD8EE, new Dictionary<int, string> {
                    {0, "ja"},
                }},
                {0xD9DE, new Dictionary<int, string> {
                    {0, "de"},
                }},
                {0xDB82, new Dictionary<int, string> {
                    {0, "fr"},
                }},
            }},
            {"spells", new Dictionary<ushort, Dictionary<int, string>> {
                {0xD996, new Dictionary<int, string> {
                    {0, "en"},
                }},
                {0xD91E, new Dictionary<int, string> {
                    {0, "ja"},
                }},
                {0xDA0E, new Dictionary<int, string> {
                    {0, "de"},
                }},
                {0xDBB2, new Dictionary<int, string> {
                    {0, "fr"},
                }},
            }},
            {"zones", new Dictionary<ushort, Dictionary<int, string>> {
                {0xD8A9, new Dictionary<int, string> {
                    {0, "en"},
                }},
                {0xD8AA, new Dictionary<int, string> {
                    {0, "search"},
                }},
                {0xD8EF, new Dictionary<int, string> {
                    {0, "ja"},
                }},
                {0xD9DF, new Dictionary<int, string> {
                    {0, "de"},
                }},
                {0xDB83, new Dictionary<int, string> {
                    {0, "fr"},
                }},
            }},
        };

        private static string Dir { get; set; }

        private static void Main()
        {
#if !DEBUG
            try
            {
#endif
                Console.CursorVisible = false;

                model = new ModelObject();
                foreach (var category in categories)
                {
                    model[category] = new List<dynamic>();
                }

                ResourceParser.Initialize(model);

                Dir = GetBaseDirectory();
                Console.WriteLine();
                if (Dir != null)
                {
                    LoadItemData();     // Items, Monstrosity
                    LoadMainData();     // Abilities, Spells

                    ParseStringTables();
                    Console.WriteLine();

                    PostProcess();
                    Console.WriteLine();

                    ApplyFixes();
                    Console.WriteLine();

                    WriteData();
                    Console.WriteLine();

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
            Console.WriteLine();

            Console.Write("Press any key to exit. ");
            Console.CursorVisible = true;
            Console.ReadKey(true);
        }

        private static void PostProcess()
        {
            Console.WriteLine("Post-processing parsed data...");

            bool success = false;
            try
            {
                // Add log names for non-english languages
                foreach (var buff in model.buffs)
                {
                    buff.jal = buff.ja;
                    buff.del = buff.de;
                    buff.frl = buff.fr;
                }

                // Populate ability recast table with proper names
                foreach (var recast in model.ability_recasts)
                {
                    foreach (var action in model.actions)
                    {
                        if (recast.id == action.recast_id)
                        {
                            recast.en = action.en;
                            recast.ja = action.ja;
                            // Remove this after July 2014
                            recast.de = action.de;
                            recast.fr = action.fr;
                        }
                    }
                }

                // Populate spell recast table with proper names
                foreach (var recast in model.spell_recasts)
                {
                    foreach (var spell in model.spells)
                    {
                        if (recast.id == spell.recast_id)
                        {
                            recast.en = spell.en;
                            recast.ja = spell.ja;
                            // Remove this after July 2014
                            recast.de = spell.de;
                            recast.fr = spell.fr;
                        }
                    }
                }

                // Split abilities into categories
                foreach (var action in model.actions)
                {
                    // Weapon skill
                    if (action.id >= 0x0000 && action.id < 0x0200)
                    {
                        action.monster_level = null;
                        action.mp_cost = null;
                        action.recast_id = null;
                        action.tp_cost = null;
                        action.type = null;

                        model.weapon_skills.Add(action);
                    }
                    // Job ability
                    else if (action.id >= 0x0200 && action.id < 0x0600)
                    {
                        action.id -= 0x0200;

                        action.monster_level = null;

                        model.job_abilities.Add(action);
                    }
                    // Job traits
                    else if (action.id >= 0x0600 && action.id < 0x0700)
                    {
                        action.id -= 0x0600;

                        action.monster_level = null;
                        action.mp_cost = null;
                        action.prefix = null;
                        action.recast_id = null;
                        action.tp_cost = null;
                        action.type = null;

                        model.job_traits.Add(action);
                    }
                    // Monstrosity
                    else if (action.id >= 0x0700)
                    {
                        action.id -= 0x0700;
                        action.id += 0x0100;

                        action.en = null;
                        action.ja = null;
                        action.de = null;
                        action.fr = null;
                        action.mp_cost = null;
                        action.recast_id = null;
                        action.type = null;

                        if (action.id - 0x100 < model.monster_abilities.Count)
                        {
                            model.monster_abilities[action.id - 0x100].Merge(action);
                        }
                    }
                }
                model.actions = null;

                // Add categories to key items
                var category = "";
                for (var i = model.key_items.Count - 1; i >= 0; --i)
                {
                    dynamic ki = model.key_items[i];
                    if (ki.en.StartsWith("-"))
                    {
                        category = ki.en.Substring(1);
                        model.key_items.Remove(ki);
                    }
                    else
                    {
                        ki.category = category;
                    }
                }

                success = true;
            }
            finally
            {
                DisplayResult(success);
            }
        }

        private static void WriteData()
        {
            Directory.CreateDirectory("resources");
            foreach (var dir in new string[] { "lua", "xml", "json" })
            {
                string path = "resources/" + dir;
                Directory.CreateDirectory(path);
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    File.Delete(file);
                }
            }

            // Create manifest file
            XDocument manifest = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("manifest"));

            var IgnoreStrings = new Dictionary<string, string[]>();
            IgnoreStrings["buffs"] = new string[] { "(None)", "(Imagery)" };
            IgnoreStrings["zones"] = new string[] { "none" };
            foreach (var pair in model)
            {
                if (IgnoreStrings.ContainsKey(pair.Key))
                {
                    Extract(pair.Key, IgnoreStrings[pair.Key]);
                }
                else
                {
                    Extract(pair.Key);
                }

                var element = new XElement("file");
                element.Value = pair.Key;
                manifest.Root.Add(element);
            }

            manifest.Root.ReplaceNodes(manifest.Root.Elements().OrderBy(e => e.Value));
            manifest.Save(Path.Combine("resources", "manifest.xml"));
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
                DisplayResult(Dir != null);
            }

            return Dir;
        }

        private static void Extract(string name, string[] ignore = null)
        {
            DisplayMessage("Generating files for " + name + "...");

#if !DEBUG
            try
            {
#endif
                XDocument xml = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(name));
                LuaFile lua = new LuaFile(name);

                foreach (dynamic obj in model[name])
                {
                    if (IsValidName(ignore ?? new string[] { }, obj))
                    {
                        XElement xmlelement = new XElement("o");
                        foreach (var pair in obj)
                        {
                            xmlelement.SetAttributeValue(pair.Key, pair.Value);
                        }

                        xml.Root.Add(xmlelement);
                        lua.Add(obj);
                    }
                }

                xml.Root.ReplaceNodes(xml.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                xml.Save(Path.Combine("resources", "xml", string.Format(CultureInfo.InvariantCulture, "{0}.xml", name)));
                lua.Save();

#if !DEBUG
            }
            catch
            {
                DisplayError();
                throw;
            }
#endif

            DisplaySuccess();
        }

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
                    if (!model.ContainsKey(fixset.Name.LocalName))
                    {
                        model[fixset.Name.LocalName] = new List<dynamic>();
                    }
                    List<dynamic> data = (List<dynamic>)model[fixset.Name.LocalName];

                    XElement update = fixset.Element("update");
                    if (update != null)
                    {
                        foreach (XElement fix in update.Elements())
                        {
                            var elements = data.Where(e => e.id == Convert.ToInt32(fix.Attribute("id").Value, CultureInfo.InvariantCulture));

                            if (!elements.Any())
                            {
                                dynamic el = new ModelObject();

                                foreach (XAttribute attr in fix.Attributes())
                                {
                                    el[attr.Name.LocalName] = attr.Parse();
                                }

                                data.Add(el);
                                continue;
                            }
                            else
                            {
                                var element = elements.Single();
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
                            ((List<dynamic>)data).RemoveAll(x => x.id == Convert.ToInt32(fix.Attribute("id").Value, CultureInfo.InvariantCulture));
                        }
                    }
                }
#if !DEBUG
            }
            catch
            {
                DisplayError();
                throw;
            }
#endif

            DisplaySuccess();
        }

        private static void LoadItemData()
        {
            model.items = new List<dynamic>();

            try
            {
                DisplayMessage("Loading item data...");

                int[][] fileids =
                    {
                        new int[] { 0x0049, 0x004A, 0x004D, 0x004C, 0x004B, 0x005B, 0xD973, 0xD974, 0xD977, 0xD975 },
                        new int[] { 0x0004, 0x0005, 0x0008, 0x0007, 0x0006, 0x0009, 0xD8FB, 0xD8FC, 0xD8FF, 0xD8FD },
                        // Remove this after July 2014
                        new int[] { 0xDA07, 0xDA08, 0xDA0B, 0xDA0A, 0xDA09, 0xDA0C, 0xD9EB, 0xD9EC, 0xD9EF, 0xD9ED },
                        new int[] { 0xDBAB, 0xDBAC, 0xDBAF, 0xDBAE, 0xDBAD, 0xDBB0, 0xDB8F, 0xDB90, 0xDB93, 0xDB91 },
                    };

                for (var i = 0; i < fileids[0].Length; ++i)
                {
                    using (FileStream stream = File.Open(GetPath(fileids[0][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (FileStream streamja = File.Open(GetPath(fileids[1][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    // Remove this after July 2014
                    using (FileStream streamde = File.Open(GetPath(fileids[2][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (FileStream streamfr = File.Open(GetPath(fileids[3][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        ResourceParser.ParseItems(stream, streamja, streamde, streamfr);
                    }
                }
            }
            finally
            {
                DisplayResult(model.items.Count != 0);
            }
        }

        private static void LoadMainData()
        {
            DisplayMessage("Loading main data stream...");
            try
            {
                ResourceParser.ParseMainStream(File.OpenRead(GetPath(0x0051)));
            }
            catch
            {
                DisplayError();
                throw;
            }

            DisplaySuccess();
        }

        private static void ParseStringTables()
        {
            foreach (var pair in DatLut)
            {
                Console.WriteLine("Loading {0} fields...", pair.Key);
                ParseFields(pair.Key);
            }
        }

        private static void ParseFields(string name)
        {
            bool result = false;

            try
            {
                dynamic[] parsed = null;

                foreach (var filepair in DatLut[name])
                {
                    using (FileStream stream = File.OpenRead(GetPath(filepair.Key)))
                    {
                        var single = DatParser.Parse(stream, filepair.Value);
                        if (parsed == null)
                        {
                            parsed = single;
                            continue;
                        }

                        for (var i = 0; i < Math.Min(parsed.Length, single.Length); ++i)
                        {
                            parsed[i].Merge(single[i]);
                        }
                    }
                }

                if (model[name].Count > 0)
                {
                    foreach (var obj in model[name])
                    {
                        obj.Merge(parsed[obj.id]);
                    }
                }
                else
                {
                    for (var i = 0; i < parsed.Length; ++i)
                    {
                        dynamic obj = new ModelObject();
                        obj.id = i;

                        obj.Merge(parsed[i]);

                        model[name].Add(obj);
                    }
                }

                result = true;
            }
            finally
            {
                DisplayResult(result);
            }
        }

        private static IList<IList<IList<object>>> LoadActionMessages()
        {
            IList<IList<IList<object>>> result = null;

            try
            {
                DisplayMessage("Loading action messages...");

                int[] fileids = new int[] { 0x1B73, 0x1B72, 0xDA28, 0xDBCC };

                result = new List<IList<IList<object>>>();

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
                DisplayResult(result != null);
            }

            return result;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static bool IsValidName(string[] ignore, dynamic res)
        {
            return !res.ContainsKey("en") || !(res.en == "."
                || string.IsNullOrWhiteSpace(res.en) || ignore.Contains((string)res.en)
                || res.en.StartsWith("#", StringComparison.Ordinal));
        }

        private static string GetPath(int id)
        {
            string ftable = Path.Combine(Dir, "FTABLE.DAT");

            using (FileStream fstream = File.OpenRead(ftable))
            {
                fstream.Position = id * 2;
                int file = fstream.ReadByte() | fstream.ReadByte() << 8;
                return Path.Combine(Dir, "ROM",
                    string.Format(CultureInfo.InvariantCulture, "{0}", file >> 7),
                    string.Format(CultureInfo.InvariantCulture, "{0}.DAT", file & 0x7F));
            }
        }

        public static void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        public static void DisplayError()
        {
            DisplayResult("Error!", ConsoleColor.Red);
        }

        public static void DisplaySuccess()
        {
            DisplayResult("Done!", ConsoleColor.Green);
        }

        public static void DisplayResult(bool success)
        {
            if (success)
            {
                DisplaySuccess();
            }
            else
            {
                DisplayError();
            }
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
