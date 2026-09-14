using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.Win32;
using ResourceExtractor.Serializers;

namespace ResourceExtractor;

internal class Program {
	private static IDictionary<string, List<ModelObject>> Data;

	private static readonly string[] Categories = [
		"action_messages",
		"actions",
		"ability_recasts",
		"items",
		"job_abilities",
		"job_traits",
		"jobs",
		"monster_skills",
		"monstrosity",
		"spells",
		"weapon_skills",
	];

	private static readonly Dictionary<string, Dictionary<ushort, Dictionary<int, string>>> DatLut = new() {
		//TODO: Comment in once special char parsing has been added

		//["action_messages"] = new() {
		//	[0x1B73] = new() {
		//		{0, "en"},
		//	},
		//	[0x1B72] = new() {
		//		{0, "ja"},
		//	},
		//},
		["actions"] = new() {
			[0xD995] = new() {
				[0] = "en",
			},
			[0xD91D] = new() {
				[0] = "ja",
			},
		},
		["augments"] = new() {
			[0xD98C] = new() {
				[0] = "en",
			},
			[0xD914] = new() {
				[0] = "ja",
			},
		},
		["auto_translates"] = new() {
			[0xD971] = new() {
				[0] = "en",
			},
			[0xD8F9] = new() {
				[0] = "ja",
			},
		},
		["buffs"] = new() {
			[0xD9AD] = new() {
				[0] = "en",
				[1] = "enl",
			},
			[0xD935] = new() {
				[0] = "ja",
			},
		},
		["job_points"] = new() {
			[0xD98E] = new() {
				[0] = "en",
			},
			[0xD916] = new() {
				[0] = "ja",
			},
		},
		["jobs"] = new() {
			[0xD8AB] = new() {
				[0] = "en",
			},
			[0xD8AC] = new() {
				[0] = "ens",
			},
			[0xD8F0] = new() {
				[0] = "ja",
			},
		},
		["key_items"] = new() {
			[0xD98F] = new() {
				[0] = "id_en",
				[4] = "en",
				//[6] = "endesc",
			},
			[0xD917] = new() {
				[0] = "id_ja",
				[1] = "ja",
				//[2] = "jadesc",
			},
		},
		["merit_points"] = new() {
			[0xD986] = new() {
				[0] = "en",
			},
			[0xD90E] = new() {
				[0] = "ja",
			},
		},
		["monster_abilities"] = new() {
			[0x1B7B] = new() {
				[0] = "en",
			},
			[0x1B7A] = new() {
				[0] = "ja",
			},
		},
		["mounts"] = new() {
			[0xD981] = new() {
				[0] = "en",
				[1] = "icon_id",
			},
			[0xD909] = new() {
				[0] = "ja",
			},
			[0xD982] = new() {
				[0] = "endesc",
			},
			[0xD90A] = new() {
				[0] = "jadesc",
			},
		},
		["regions"] = new() {
			[0xD966] = new() {
				[0] = "en",
			},
			[0xD8EE] = new() {
				[0] = "ja",
			},
		},
		["spells"] = new() {
			[0xD996] = new() {
				[0] = "en",
			},
			[0xD91E] = new() {
				[0] = "ja",
			},
		},
		["titles"] = new() {
			[0xD998] = new() {
				[0] = "en",
			},
			[0xD920] = new() {
				[0] = "ja",
			},
		},
		["zones"] = new() {
			[0xD8A9] = new() {
				[0] = "en",
			},
			[0xD8AA] = new() {
				[0] = "search",
			},
			[0xD8EF] = new() {
				[0] = "ja",
			},
		},
	};

	private static bool UpdateResourcesRepo { get; set; }
	private static string FfxiDir { get; set; }
	private static string OutDir { get; set; }

	private static void Main(string[] args) {
		try {
			Console.CursorVisible = false;

			Initialize(args);
			Console.WriteLine();

			ResourceParser.Initialize(Data);

			LoadItemData(); // Items, Monstrosity
			LoadMainData(); // Abilities, Spells

			ParseStringTables();
			Console.WriteLine();

			PostProcess();
			Console.WriteLine();

			ApplyFixes();
			Console.WriteLine();

			// Clear directories
			if (UpdateResourcesRepo) {
				foreach (var file in Directory.EnumerateFiles(OutDir)) {
					File.Delete(file);
				}
			} else {
				Directory.CreateDirectory("resources");
				foreach (var path in new[] { "lua", "xml", "json", "maps" }.Select(dir => "resources/" + dir)) {
					Directory.CreateDirectory(path);
					foreach (var file in Directory.EnumerateFiles(path)) {
						File.Delete(file);
					}
				}
			}

			WriteData();
			Console.WriteLine();

			if (args.Contains("--maps") || args.Contains("-m")) {
				ExtractMaps();
				Console.WriteLine();
			}

			if (args.Contains("--analysis") || args.Contains("-a")) {
				Analyzer.Analyze(Data);
				Console.WriteLine();
			}

			Console.WriteLine("Resource extraction complete!");
		} catch {
			if (System.Diagnostics.Debugger.IsAttached) {
				throw;
			}
		} finally {
			Console.CursorVisible = true;
		}
	}

	private static void PostProcess() {
		Console.WriteLine("Post-processing parsed data...");

		var success = false;
		try {
			// Add log names for non-english languages
			foreach (dynamic buff in Data["buffs"]) {
				if (buff.ContainsKey("ja")) {
					buff.jal = buff.ja;
				}
			}

			// Populate ability recast table with proper names
			foreach (dynamic recast in Data["ability_recasts"]) {
				foreach (dynamic action in Data["actions"]) {
					if (recast.Id != action.recast_id) {
						continue;
					}

					recast.en = action.en;
					recast.ja = action.ja;
					break;
				}
			}

			// Trust party member name defaults:
			foreach (dynamic spell in Data["spells"]) {
				if (!(spell.ContainsKey("type") && spell.type == MagicType.Trust && spell.ContainsKey("en"))) {
					continue;
				}

				spell.party_name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spell.en).Replace(" (UC)", "").Replace(" II", "").Replace(" ", "");
			}

			// Add categories to key items and synchronize IDs between languages for key items
			var category = "";
			var keyItems = new Dictionary<int, ModelObject>();
			for (var i = Data["key_items"].Count - 1; i >= 0; --i) {
				dynamic ki = Data["key_items"][i];
				if (ki.en.StartsWith("-")) {
					category = ki.en.Substring(1);
				} else if (ki.id_en != 0) {
					dynamic en = keyItems.TryGetValue((int) ki.id_en, out var entry_en) ? entry_en : [];
					en.Id = ki.id_en;
					en.en = ki.en;
					en.category = category;
					keyItems[en.Id] = en;
				}

				if (ki.id_ja != 0) {
					dynamic ja = keyItems.TryGetValue((int) ki.id_ja, out var entry_ja) ? entry_ja : [];
					ja.ja = ki.ja;
					keyItems[ki.id_ja] = ja;
				}
			}
			Data["key_items"] = [.. keyItems.Values.OrderBy(key_item => key_item.Id)];

			// Move item descriptions into separate table
			Data["item_descriptions"] = [];
			foreach (dynamic item in Data["items"]) {
				dynamic description = new ModelObject(item.Id);
				description.en = item.endesc;
				description.ja = item.jadesc;

				item.endesc = null;
				item.jadesc = null;

				Data["item_descriptions"].Add(description);
			}

			var ammoSlot = 8;
			var corJobFlag = 1 << 17;
			var rangeSlotSkills = new Dictionary<int, string>() {
				[25] = "Archery",
				[26] = "Marksmanship",
				[48] = "Fishing",
			};
			foreach (dynamic item in Data["items"]) {
				if (!item.ContainsKey("slots") || !item.ContainsKey("skill") || !rangeSlotSkills.ContainsKey(item.skill)) {
					continue;
				}

				var skill = rangeSlotSkills[item.skill];
				var ammo = item.slots == ammoSlot;
				if (skill == "Archery") {
					if (ammo) {
						item.ammo_type = "Arrow";
					} else {
						item.range_type = "Bow";
					}
				} else if (skill == "Marksmanship") {
					if (ammo) {
						if (item.ja.Contains("ブレット") || item.en.Contains("Bullet")) {
							item.ammo_type = "Bullet";
						} else if (item.ja.Contains("シェル") || item.en.Contains("Shell")) {
							item.ammo_type = "Shell";
						} else if (item.ja.Contains("ボルト") || item.en.Contains("Bolt")) {
							item.ammo_type = "Bolt";
						}
					} else {
						if (item.delay >= 700 && item.delay < 999) {
							item.range_type = "Cannon";
						} else if (item.delay > 450 || (item.jobs & corJobFlag) == corJobFlag) {
							item.range_type = "Gun";
						} else {
							item.range_type = "Crossbow";
						}
					}
				} else if (skill == "Fishing") {
					if (ammo) {
						item.ammo_type = "Bait";
					} else {
						item.range_type = "Fishing Rod";
					}
				}
			}

			// Move item grammar into separate table
			Data["items_grammar"] = [];
			foreach (dynamic item in Data["items"]) {
				if (item.en != "." && item.Id is < 29681 or > 29693) {
					dynamic grammar = new ModelObject(item.Id);
					grammar.plural = item.enlp;
					grammar.article = item.art;

					item.art = null;
					item.enlp = null;

					Data["items_grammar"].Add(grammar);
				}
			}

			// Fill in linked auto-translate names
			foreach (dynamic at in Data["auto_translates"]) {
				if (!at.en.StartsWith("@")) {
					continue;
				}

				var id = Int32.Parse(at.en.Substring(2), NumberStyles.HexNumber);
				var key = (char) at.en[1] switch {
					'A' => "zones",
					'C' => "spells",
					'J' => "jobs",
					'Y' => "actions",
					_ => throw new InvalidDataException(String.Format("Unknown auto-translate code: {0}", at.en)),
				};

				dynamic item = null;
				foreach (var i in Data[key]) {
					if (i.Id != id) {
						continue;
					}

					item = i;
					break;
				}

				if (item == null) {
					throw new InvalidDataException(String.Format("Unknown auto-translate ID for {0}: {1}", key, id));
				}

				at.en = item.en;
				at.ja = item.ja;
			}

			// Split abilities into categories
			foreach (dynamic action in Data["actions"]) {
				// Weapon skill
				if (action.Id is >= 0x0000 and < 0x0200) {
					action.monster_level = null;
					action.mp_cost = null;
					action.recast_id = null;
					action.tp_cost = null;
					action.type = null;

					Data["weapon_skills"].Add(action);
				}
				// Job ability
				else if (action.Id is >= 0x0200 and < 0x0600) {
					action.Id -= 0x0200;

					action.monster_level = null;

					Data["job_abilities"].Add(action);
				}
				// Job traits
				else if (action.Id is >= 0x0600 and < 0x0700) {
					action.Id -= 0x0600;

					action.monster_level = null;
					action.mp_cost = null;
					action.prefix = null;
					action.recast_id = null;
					action.tp_cost = null;
					action.type = null;

					Data["job_traits"].Add(action);
				}
				// Monstrosity
				else if (action.Id is >= 0x0700) {
					// Monster skills start at 256
					action.Id -= 0x0600;

					action.mp_cost = null;
					action.recast_id = null;
					action.type = null;

					Data["monster_skills"].Add(action);
				}
			}
			Data.Remove("actions");

			// Shift monster abilities up by 0x100
			foreach (var monsterAbility in Data["monster_abilities"]) {
				monsterAbility.Id += 0x100;
			}

			// Split merit point names/descriptions and filter garbage values
			foreach (dynamic meritPoint in Data["merit_points"]) {
				// The first 64 entries contain the category names
				if (meritPoint.Id < 0x40 || meritPoint.en.StartsWith("Meripo")) {
					meritPoint.Id = 0;
					continue;
				}

				// Uneven entries contain the descriptions for the previous entry
				if (meritPoint.Id % 2 != 1) {
					continue;
				}

				Data["merit_points"][meritPoint.Id - 1].endesc = meritPoint.en;
				Data["merit_points"][meritPoint.Id - 1].jadesc = meritPoint.ja;

				meritPoint.Id = 0;
			}

			Data["merit_points"].RemoveAll(meritPoint => meritPoint.Id == 0);

			// Split job point names/descriptions and filter garbage values
			foreach (dynamic jobPoint in Data["job_points"]) {
				// The first 64 entries contain the category names
				if (jobPoint.Id is < 0x40 || jobPoint.en is "カテゴリー名" or "ヘルプ文") {
					jobPoint.Id = 0;
					continue;
				}

				// Uneven entries contain the descriptions for the previous entry
				if (jobPoint.Id % 2 != 1) {
					continue;
				}

				Data["job_points"][jobPoint.Id - 1].endesc = jobPoint.en;
				Data["job_points"][jobPoint.Id - 1].jadesc = jobPoint.ja;

				jobPoint.Id = 0;
			}

			Data["job_points"].RemoveAll(jobPoint => jobPoint.Id == 0);

			foreach (dynamic mount in Data["mounts"]) {
				mount.prefix = "/mount";
			}

			success = true;
		} finally {
			DisplayResult(success);
		}
	}

	private static readonly IDictionary<string, string[]> IgnoreStrings = new Dictionary<string, string[]> {
		["buffs"] = ["(None)", "(Imagery)"],
		["titles"] = ["0"],
		["zones"] = ["none"]
	};

	internal static bool IsValidObject(string name, ModelObject obj) {
		return IsValidName(IgnoreStrings.TryGetValue(name, out var value) ? value : [], obj);
	}

	private static void WriteData() {
		// Create manifest file
		var manifest = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("manifest"));

		foreach (var (name, resources) in Data) {
			if (IgnoreStrings.TryGetValue(name, out var ignored)) {
				Extract(name, ignored);
			} else {
				Extract(name);
			}

			var element = new XElement("file") {
				Value = name,
			};
			manifest.Root.Add(element);
		}

		manifest.Root.ReplaceNodes(manifest.Root.Elements().OrderBy(e => e.Value));
		manifest.Save(Path.Combine("resources", "manifest.xml"));
	}

	private static void Initialize(string[] args) {
		UpdateResourcesRepo = args.Contains("--update-repo") || args.Contains("-r");

		InitializeBaseDirectory(args);
		InitializeOutDirectory(args);

		var data = new Dictionary<string, List<ModelObject>>();
		foreach (var category in Categories) {
			data[category] = [];
		}
		foreach (var pair in DatLut) {
			data[pair.Key] = [];
		}
		Data = data;
	}

	private static void InitializeBaseDirectory(string[] args) {
		DisplayMessage("Locating Final Fantasy XI installation directory...");

		var message = "Unable to locate Final Fantasy XI installation.";
		try {
			if (OperatingSystem.IsWindows()) {
				using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
				using var key = (hklm.OpenSubKey("SOFTWARE\\PlayOnlineUS\\InstallFolder") ?? hklm.OpenSubKey("SOFTWARE\\PlayOnline\\InstallFolder")) ?? hklm.OpenSubKey("SOFTWARE\\PlayOnlineEU\\InstallFolder");
				FfxiDir = key?.GetValue("0001") as string;
			} else {
				FfxiDir = args.FirstOrDefault(arg => arg[0] != '-');
				if (FfxiDir == null) {
					message = "No path provided";
				}
			}

			if (FfxiDir != null && !Directory.Exists(FfxiDir)) {
				message = $"Path \"{FfxiDir}\" not found.";
				FfxiDir = null;
			}
		} catch (Exception ex) {
			message = ex.Message;
		} finally {
			var success = FfxiDir != null;
			DisplayResult(success);
			if (!success) {
				DisplayMessage(message);
				Environment.Exit(1);
			}
		}
	}

	private static void InitializeOutDirectory(string[] args) {
		DisplayMessage("Setting output directory...");

		var message = "Could not determine output directory.";
		try {
			var outIndex = args.IndexOf("--out") ?? args.IndexOf("-o");
			if (outIndex == null) {
				OutDir = Directory.GetCurrentDirectory();
				return;
			}

			if (outIndex == args.Length - 1) {
				message = "No output directory provided.";
				return;
			}

			OutDir = args[outIndex.Value + 1];
			if (UpdateResourcesRepo) {
				if (!Directory.Exists(OutDir)) {
					message = "Specified repository directory does not exist.";
					OutDir = null;
				}
			} else {
				Directory.CreateDirectory(OutDir);
			}
		} catch (Exception ex) {
			message = ex.Message;
		} finally {
			var success = OutDir != null;
			DisplayResult(success);
			if (!success) {
				DisplayMessage(message);
				Environment.Exit(1);
			}
		}
	}

	private static void Extract(string name, string[] ignore = null) {
		DisplayMessage("Generating files for " + name + "...");

		try {
			ignore ??= [];

			var entries = Data[name].Where(obj => IsValidName(ignore, obj)).ToArray();

			if (UpdateResourcesRepo) {
				LuaFile.Write(OutDir, name, entries);
			} else {
				LuaFile.Write(Path.Combine(OutDir, "resources", "lua"), name, entries);
				var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(name));
				foreach (var obj in Data[name]) {
					if (!IsValidName(ignore, obj)) {
						continue;
					}

					var xmlelement = new XElement("o");
					foreach (var pair in obj) {
						//TODO: Level dictionaries are currently messed up on XML output
						if (pair.Key.StartsWith('_')) {
							continue;
						}

						if (pair.Value is not string && pair.Value is IEnumerable) {
							xmlelement.SetAttributeValue(pair.Key, LuaAttribute.MakeValue(pair.Value));
						} else {
							xmlelement.SetAttributeValue(pair.Key, pair.Value);
						}
					}

					xml.Root.Add(xmlelement);
				}
				xml.Root.ReplaceNodes(xml.Root.Elements().OrderBy(e => (uint) ((int?) e.Attribute("id") ?? 0)));
				xml.Save(Path.Combine(OutDir, "resources", "xml", FormattableString.Invariant($"{name}.xml")));
			}
		} catch {
			DisplayError();
			throw;
		}

		DisplaySuccess();
	}

	private static void ApplyFixes() {
		DisplayMessage("Applying fixes...");
		try {
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ResourceExtractor.fixes.xml");
			var fixes = XDocument.Load(stream);

			foreach (var fixset in fixes.Root.Elements()) {
				if (!Data.TryGetValue(fixset.Name.LocalName, out var resources)) {
					resources = [];
					Data[fixset.Name.LocalName] = resources;
				}

				var update = fixset.Element("update");
				if (update != null) {
					foreach (var fix in update.Elements()) {
						var element = resources.SingleOrDefault(e => e.Id == Convert.ToInt32(fix.Attribute("id").Value, CultureInfo.InvariantCulture));

						if (element == null) {
							element = new ModelObject(Int32.Parse(fix.Attribute("id").Value));

							resources.Add(element);
						}

						foreach (var attr in fix.Attributes().Where(attribute => attribute.Name != "id")) {
							element[attr.Name.LocalName] = attr.Value.Parse();
						}
					}
				}

				var remove = fixset.Element("remove");
				if (remove == null) {
					continue;
				}

				foreach (var fix in remove.Elements()) {
					resources.RemoveAll(x => x.Id == Convert.ToInt32(fix.Attribute("id").Value, CultureInfo.InvariantCulture));
				}
			}
		} catch {
			DisplayError();
			throw;
		}

		DisplaySuccess();
	}

	private static void LoadItemData() {
		Data["items"] = [];

		try {
			DisplayMessage("Loading item data...");

			int[][] fileids = [
			//                           Armor   Weapons
				[0x0049, 0x004A, 0x004D, 0x004C, 0x004B, 0x005B, 0xD973, 0xD974, 0xD977, 0xD975],
				[0x0004, 0x0005, 0x0008, 0x0007, 0x0006, 0x0009, 0xD8FB, 0xD8FC, 0xD8FF, 0xD8FD],
			];

			for (var i = 0; i < fileids[0].Length; ++i) {
				using var stream = File.Open(GetPath(fileids[0][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var streamja = File.Open(GetPath(fileids[1][i]), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				ResourceParser.ParseItems(stream, streamja);
			}
		} finally {
			DisplayResult(Data["items"].Count != 0);
		}
	}

	private static void LoadMainData() {
		DisplayMessage("Loading main data stream...");
		try {
			using var file = File.OpenRead(GetPath(0x0051));
			ResourceParser.ParseMainStream(file);
		} catch {
			DisplayError();
			throw;
		}

		DisplaySuccess();
	}

	private static void ParseStringTables() {
		foreach (var pair in DatLut) {
			Console.WriteLine("Loading {0} fields...", pair.Key);
			ParseFields(pair.Key);
		}
	}

	private static void ParseFields(string name) {
		var result = false;

		try {
			ModelObject[] parsed = null;

			foreach (var filepair in DatLut[name]) {
				using var stream = File.OpenRead(GetPath(filepair.Key));
				var single = DatParser.Parse(stream, filepair.Value);
				if (parsed == null) {
					parsed = single;
					continue;
				}

				for (var i = 0; i < Math.Min(parsed.Length, single.Length); ++i) {
					parsed[i].Merge(single[i]);
				}
			}

			if (parsed == null) {
				throw new InvalidDataException($"Could not parse data for field \"{name}\".");
			}

			if (Data[name].Count > 0) {
				foreach (var item in Data[name]) {
					item.Merge(parsed[item.Id]);
				}
			} else if (parsed.Any(entry => entry.Id != 0)) {
				Data[name] = [.. parsed];
			} else {
				for (var i = 0; i < parsed.Length; ++i) {
					dynamic obj = new ModelObject(i);

					obj.Merge(parsed[i]);

					Data[name].Add(obj);
				}
			}

			result = true;
		} finally {
			DisplayResult(result);
		}
	}

	private static bool IsValidName(string[] ignore, dynamic res) =>
		// English
		(!res.ContainsKey("en") || !(
			res.en == "." ||
			String.IsNullOrWhiteSpace(res.en) ||
			ignore.Contains((string) res.en) ||
			res.en.StartsWith("#", StringComparison.Ordinal)
		)) &&
		// Japanese
		(!res.ContainsKey("ja") || !(
			res.ja == "." ||
			String.IsNullOrWhiteSpace(res.ja) ||
			ignore.Contains((string) res.ja) ||
			res.ja.StartsWith("#", StringComparison.Ordinal)
		));

	private static void ExtractMaps() {
		DisplayMessage("Extracting map data...");
		if (!OperatingSystem.IsWindowsVersionAtLeast(7)) {
			DisplayError("Map extraction currently only works on Windows 7 and higher.");
			return;
		}

		try {
			MapParser.Extract(OutDir);
			DisplaySuccess();
		} catch {
			DisplayError();
		}
	}

	public static string GetPath(int id) {
		var ftable = Path.Combine(FfxiDir, "FTABLE.DAT");

		using var fstream = File.OpenRead(ftable);
		fstream.Position = id * 2;
		var file = fstream.ReadByte() | fstream.ReadByte() << 8;
		return Path.Combine(FfxiDir, "ROM",
			String.Format(CultureInfo.InvariantCulture, "{0}", file >> 7),
			String.Format(CultureInfo.InvariantCulture, "{0}.DAT", file & 0x7F));
	}

	public static void DisplayMessage(string message) {
		Console.WriteLine(message);
	}

	public static void DisplayMessage(string message, ConsoleColor color) {
		var backupColor = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.WriteLine(message);
		Console.ForegroundColor = backupColor;
	}

	public static void DisplayError(string message = null) {
		DisplayResult("Error!", ConsoleColor.Red);
		if (message != null) {
			Console.WriteLine();
			DisplayMessage($"  {message}", ConsoleColor.Red);
			Console.WriteLine();
		}
	}

	public static void DisplaySuccess() {
		DisplayResult("Done!", ConsoleColor.Green);
	}

	public static void DisplayResult(bool success) {
		if (success) {
			DisplaySuccess();
		} else {
			DisplayError();
		}
	}

	private static void DisplayResult(string result, ConsoleColor color) {
		Console.CursorTop -= 1;
		Console.CursorLeft = Console.BufferWidth - result.Length - 2;
		DisplayMessage($"[{result}]", color);
	}
}
