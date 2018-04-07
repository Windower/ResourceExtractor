// <copyright file="Program.cs" company="Windower Team">
// Copyright © 2013-2018 Windower Team
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Microsoft.Win32;
    using Serializers.Lua;

    internal class Program
    {
        private static dynamic model;
        private static readonly string[] categories = {
            "action_messages",
            "actions",
            "ability_recasts",
            "items",
            "job_abilities",
            "job_traits",
            "jobs",
            "monstrosity",
            "spells",
            "weapon_skills",
        };

        private static readonly Dictionary<string, IDictionary<ushort, IDictionary<int, string>>> DatLut = new Dictionary<string, IDictionary<ushort, IDictionary<int, string>>> {
            //TODO: Comment in once special char parsing has been added

            //["action_messages"] = new Dictionary<ushort, IDictionary<int, string>> {
            //    [0x1B73] = new Dictionary<int, string> {
            //        {0, "en"},
            //    },
            //    [0x1B72] = new Dictionary<int, string> {
            //        {0, "ja"},
            //    },
            //},
            ["actions"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD995] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD91D] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["augments"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD98C] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD914] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["auto_translates"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD971] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD8F9] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["buffs"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD9AD] = new Dictionary<int, string> {
                    [0] = "en",
                    [1] = "enl",
                },
                [0xD935] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["job_points"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD98E] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD916] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["jobs"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD8AB] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD8AC] = new Dictionary<int, string> {
                    [0] = "ens",
                },
                [0xD8F0] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["key_items"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD98F] = new Dictionary<int, string> {
                    [0] = "id",
                    [4] = "en",
                    //[6] = "endesc",
                },
                [0xD917] = new Dictionary<int, string> {
                    [1] = "ja",
                    //[2] = "jadesc",
                },
            },
            ["merit_points"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD986] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD90E] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["monster_abilities"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0x1B7B] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0x1B7A] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["mounts"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD981] = new Dictionary<int, string> {
                    [0] = "en",
                    [1] = "icon_id",
                },
                [0xD909] = new Dictionary<int, string> {
                    [0] = "ja",
                },
                [0xD982] = new Dictionary<int, string> {
                    [0] = "endesc",
                },
                [0xD90A] = new Dictionary<int, string> {
                    [0] = "jadesc",
                },
            },
            ["regions"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD966] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD8EE] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["spells"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD996] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD91E] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["titles"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD998] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD920] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
            ["zones"] = new Dictionary<ushort, IDictionary<int, string>> {
                [0xD8A9] = new Dictionary<int, string> {
                    [0] = "en",
                },
                [0xD8AA] = new Dictionary<int, string> {
                    [0] = "search",
                },
                [0xD8EF] = new Dictionary<int, string> {
                    [0] = "ja",
                },
            },
        };

        private static string Dir { get; set; }

        private static void Main(String[] args)
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
                foreach (var pair in DatLut)
                {
                    model[pair.Key] = new List<dynamic>();
                }

                ResourceParser.Initialize(model);

                Dir = GetBaseDirectory();
                Console.WriteLine();
                if (Dir == null)
                {
                    Console.WriteLine("Unable to locate Final Fantasy XI installation.");
                    Console.WriteLine();
                    return;
                }

                LoadItemData();     // Items, Monstrosity
                LoadMainData();     // Abilities, Spells

                ParseStringTables();
                Console.WriteLine();

                PostProcess();
                Console.WriteLine();

                ApplyFixes();
                Console.WriteLine();

                // Clear directories
                Directory.CreateDirectory("resources");
                foreach (var path in new [] { "lua", "xml", "json", "maps" }.Select(dir => "resources/" + dir))
                {
                    Directory.CreateDirectory(path);
                    foreach (var file in Directory.EnumerateFiles(path))
                    {
                        File.Delete(file);
                    }
                }

                WriteData();
                Console.WriteLine();

                if (args.Contains("--maps") || args.Contains("-m"))
                {
                    MapParser.Extract();
                    Console.WriteLine();
                }

                if (args.Contains("--analysis") || args.Contains("-a"))
                {
                    Analyzer.Analyze(model);
                }

                Console.WriteLine("Resource extraction complete!");
#if !DEBUG
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    throw;
                }
            }
#endif
            Console.WriteLine();
        }

        private static void PostProcess()
        {
            Console.WriteLine("Post-processing parsed data...");

            var success = false;
            try
            {
                // Add log names for non-english languages
                foreach (var buff in model.buffs)
                {
                    if (buff.ContainsKey("ja"))
                    {
                        buff.jal = buff.ja;
                    }
                }

                // Populate ability recast table with proper names
                foreach (var recast in model.ability_recasts)
                {
                    foreach (var action in model.actions)
                    {
                        if (recast.id != action.recast_id)
                        {
                            continue;
                        }

                        recast.en = action.en;
                        recast.ja = action.ja;
                        break;
                    }
                }

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

                // Move item descriptions into separate table
                //TODO: Remove when shared resources are implemented
                model.item_descriptions = new List<dynamic>();
                foreach (var item in model.items)
                {
                    dynamic item_description = new ModelObject();
                    item_description.id = item.id;
                    item_description.en = item.endesc;
                    item_description.ja = item.jadesc;

                    item.endesc = null;
                    item.jadesc = null;

                    model.item_descriptions.Add(item_description);
                }

                // Fill in linked auto-translate names
                foreach (var at in model.auto_translates)
                {
                    if (!at.en.StartsWith("@"))
                    {
                        continue;
                    }

                    int id = int.Parse(at.en.Substring(2), NumberStyles.HexNumber);

                    string key;
                    switch ((char)at.en[1])
                    {
                        case 'A':
                            key = "zones";
                            break;
                        case 'C':
                            key = "spells";
                            break;
                        case 'J':
                            key = "jobs";
                            break;
                        case 'Y':
                            key = "actions";
                            break;
                        default:
                            throw new InvalidDataException(string.Format("Unknown auto-translate code: {0}", at.en));
                    }
                        
                    dynamic item = null;
                    foreach (var i in model[key])
                    {
                        if (i.id != id)
                        {
                            continue;
                        }

                        item = i;
                        break;
                    }

                    if (item != null)
                    {
                        at.en = item.en;
                        at.ja = item.ja;
                    }
                    else
                    {
                        //throw new InvalidDataException(string.Format("Unknown auto-translate ID for {0}: {1}", key, id));
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

                        action.mp_cost = null;
                        action.recast_id = null;
                        action.type = null;

                        // Remove names, as they are parsed separately
                        action.en = null;
                        action.ja = null;

                        if (action.id < model.monster_abilities.Count)
                        {
                            model.monster_abilities[action.id].Merge(action);
                        }
                    }
                }
                model.actions = null;


                // Shift monster abilities up by 0x100
                foreach (var monster_ability in model.monster_abilities)
                {
                    monster_ability.id += 0x100;
                }

                // Split merit point names/descriptions and filter garbage values
                foreach (var merit_point in model.merit_points)
                {
                    // The first 64 entries contain the category names
                    if (merit_point.id < 0x40 || merit_point.en.StartsWith("Meripo"))
                    {
                        merit_point.id = 0;
                        continue;
                    }

                    // Uneven entries contain the descriptions for the previous entry
                    if (merit_point.id % 2 != 1)
                    {
                        continue;
                    }

                    model.merit_points[merit_point.id - 1].endesc = merit_point.en;
                    model.merit_points[merit_point.id - 1].jadesc = merit_point.ja;

                    merit_point.id = 0;
                }

                ((List<dynamic>)model.merit_points).RemoveAll(merit_point => merit_point.id == 0);

                // Split job point names/descriptions and filter garbage values
                foreach (var job_point in model.job_points)
                {
                    // The first 64 entries contain the category names
                    if (job_point.id < 0x40 || job_point.en == "カテゴリー名" || job_point.en == "ヘルプ文")
                    {
                        job_point.id = 0;
                        continue;
                    }

                    // Uneven entries contain the descriptions for the previous entry
                    if (job_point.id % 2 != 1)
                    {
                        continue;
                    }

                    model.job_points[job_point.id - 1].endesc = job_point.en;
                    model.job_points[job_point.id - 1].jadesc = job_point.ja;

                    job_point.id = 0;
                }

                ((List<dynamic>)model.job_points).RemoveAll(job_point => job_point.id == 0);

                foreach (var mount in model.mounts)
                {
                    mount.prefix = "/mount";
                }

                success = true;
            }
            finally
            {
                DisplayResult(success);
            }
        }

        private static readonly IDictionary<string, string[]> IgnoreStrings = new Dictionary<string, string[]>
        {
            ["buffs"] = new[] {"(None)", "(Imagery)"},
            ["titles"] = new[] {"0"},
            ["zones"] = new[] {"none"}
        };

        internal static Boolean IsValidObject(string name, dynamic obj)
        {
            return IsValidName(IgnoreStrings.ContainsKey(name) ? IgnoreStrings[name] : new string[] { }, obj);
        }

        private static void WriteData()
        {
            // Create manifest file
            var manifest = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("manifest"));

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

                var element = new XElement("file")
                {
                    Value = pair.Key,
                };
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
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = (hklm.OpenSubKey("SOFTWARE\\PlayOnlineUS\\InstallFolder") ?? hklm.OpenSubKey("SOFTWARE\\PlayOnline\\InstallFolder")) ?? hklm.OpenSubKey("SOFTWARE\\PlayOnlineEU\\InstallFolder"))
                    {
                        Dir = key?.GetValue("0001") as string;
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
                var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(name));
                var lua = new LuaFile(name);

                foreach (var obj in model[name])
                {
                    if (!IsValidName(ignore ?? new string[] {}, obj))
                    {
                        continue;
                    }

                    var xmlelement = new XElement("o");
                    foreach (var pair in obj)
                    {
                        //TODO: Level dictionaries are currently messed up on XML output
                        if (pair.Key.StartsWith("_"))
                        {
                            continue;
                        }
                        xmlelement.SetAttributeValue(pair.Key, pair.Value);
                    }

                    xml.Root.Add(xmlelement);
                    lua.Add(obj);
                }

                xml.Root.ReplaceNodes(xml.Root.Elements().OrderBy(e => (uint)((int?)e.Attribute("id") ?? 0)));

                xml.Save(Path.Combine("resources", "xml", FormattableString.Invariant($"{name}.xml")));
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
                var fixes = XDocument.Load("fixes.xml");
                if (fixes.Root == null)
                {
                    throw new InvalidDataException("fixes.xml file corrupted.");
                }

                foreach (var fixset in fixes.Root.Elements())
                {
                    if (!model.ContainsKey(fixset.Name.LocalName))
                    {
                        model[fixset.Name.LocalName] = new List<dynamic>();
                    }
                    var data = (List<dynamic>)model[fixset.Name.LocalName];

                    var update = fixset.Element("update");
                    if (update != null)
                    {
                        foreach (var fix in update.Elements())
                        {
                            var elements = data.Where(e => e.id == Convert.ToInt32(fix.Attribute("id").Value, CultureInfo.InvariantCulture)).ToList();

                            if (!elements.Any())
                            {
                                dynamic el = new ModelObject();

                                foreach (var attr in fix.Attributes())
                                {
                                    el[attr.Name.LocalName] = attr.Parse();
                                }

                                data.Add(el);
                                continue;
                            }

                            var element = elements.Single();
                            foreach (var attr in fix.Attributes())
                            {
                                element[attr.Name.LocalName] = attr.Parse();
                            }
                        }
                    }

                    var remove = fixset.Element("remove");
                    if (remove == null)
                    {
                        continue;
                    }

                    foreach (var fix in remove.Elements())
                    {
                        data.RemoveAll(x => x.id == Convert.ToInt32(fix.Attribute("id").Value, CultureInfo.InvariantCulture));
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
                    { //                                 Armor   Weapons
                        new [] { 0x0049, 0x004A, 0x004D, 0x004C, 0x004B, 0x005B, 0xD973, 0xD974, 0xD977, 0xD975 },
                        new [] { 0x0004, 0x0005, 0x0008, 0x0007, 0x0006, 0x0009, 0xD8FB, 0xD8FC, 0xD8FF, 0xD8FD },
                    };

                for (var i = 0; i < fileids[0].Length; ++i)
                {
                    using (var stream = File.Open(GetPath(fileids[0][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var streamja = File.Open(GetPath(fileids[1][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        ResourceParser.ParseItems(stream, streamja);
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
#if !DEBUG
            try
            {
#endif
                using (var file = File.OpenRead(GetPath(0x0051)))
                {
                    ResourceParser.ParseMainStream(file);
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
            var result = false;

            try
            {
                dynamic[] parsed = null;

                foreach (var filepair in DatLut[name])
                {
                    using (var stream = File.OpenRead(GetPath(filepair.Key)))
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

                if (parsed == null)
                {
                    throw new InvalidDataException($"Could not parse data for field \"{name}\".");
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

        [SuppressMessage("Microsoft.Maintainability", "CA1502")]
        private static bool IsValidName(string[] ignore, dynamic res)
        {
            return
                // English
                (!res.ContainsKey("en") || !(res.en == "."
                || string.IsNullOrWhiteSpace(res.en) || ignore.Contains((string)res.en)
                || res.en.StartsWith("#", StringComparison.Ordinal)))
                // Japanese
                && (!res.ContainsKey("ja") || !(res.ja == "."
                || string.IsNullOrWhiteSpace(res.ja) || ignore.Contains((string)res.ja)
                || res.ja.StartsWith("#", StringComparison.Ordinal)));
        }

        public static string GetPath(int id)
        {
            var ftable = Path.Combine(Dir, "FTABLE.DAT");

            using (var fstream = File.OpenRead(ftable))
            {
                fstream.Position = id * 2;
                var file = fstream.ReadByte() | fstream.ReadByte() << 8;
                return Path.Combine(Dir, "ROM",
                    string.Format(CultureInfo.InvariantCulture, "{0}", file >> 7),
                    string.Format(CultureInfo.InvariantCulture, "{0}.DAT", file & 0x7F));
            }
        }

        public static int GetDat(string path)
        {
            var pattern = new Regex(@".*ROM[/\\](\d+)[/\\](\d+).DAT$");
            var matches = pattern.Matches(path);
            if (matches.Count != 1 || matches[0].Groups.Count != 3)
            {
                throw new ArgumentException("Invalid path specified.");
            }
            var check = int.Parse(matches[0].Groups[1].Value) << 7 | int.Parse(matches[0].Groups[2].Value);
            var ftable = Path.Combine(Dir, "FTABLE.DAT");

            using (var fstream = File.OpenRead(ftable))
            {
                while (fstream.Position < fstream.Length)
                {
                    var file = fstream.ReadByte() | fstream.ReadByte() << 8;
                    if (file != check)
                    {
                        continue;
                    }

                    return (int) (fstream.Position/2) - 1;
                }
            }

            throw new ArgumentException("No ID found corresponding to the provided path.");
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

            var currentcolor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write($"[{result}]");
            }
            finally
            {
                Console.ForegroundColor = currentcolor;
            }
        }
    }
}
