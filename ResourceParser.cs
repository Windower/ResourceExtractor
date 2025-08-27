using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ResourceExtractor;

internal static class ResourceParser {
	private static dynamic model;

	public static void Initialize(dynamic model) {
		ResourceParser.model = model;
	}

	private enum StringIndex {
		Name = 0,
		EnglishArticle = 1,
		EnglishLogSingular = 2,
		EnglishLogPlural = 3,
		EnglishDescription = 4,
		JapaneseDescription = 1,
	}

	private enum BlockType {
		ContainerEnd = 0x00,
		ContainerBegin = 0x01,
		SpellData = 0x49,
		AbilityData = 0x53,
	}

	[SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Messes up struct layout")]
	[StructLayout(LayoutKind.Sequential)]
	private readonly struct Header {
		private readonly int id;
		private readonly int size;
		private readonly long padding;

		public int Id => id;
		public int Size => (size >> 3 & ~0xF) - 0x10;
		public BlockType Type => (BlockType) (size & 0x7F);
	}

	public static void ParseMainStream(Stream stream) {
		var header = stream.Read<Header>();
		var block = stream.Position;

		if (header.Type != BlockType.ContainerBegin) {
			throw new InvalidDataException();
		}

		stream.Position = block + header.Size;

		while (header.Type != BlockType.ContainerEnd) {
			header = stream.Read<Header>();
			block = stream.Position;

			LoadStreamItem(stream, header);
			stream.Position = block + header.Size;
		}
	}

	private static void LoadStreamItem(Stream stream, Header header) {
		switch (header.Type) {
			case BlockType.ContainerEnd:
				break;
			case BlockType.ContainerBegin:
				stream.Position -= Marshal.SizeOf(typeof(Header));
				ParseMainStream(stream);
				break;
			case BlockType.SpellData:
				ParseSpells(stream, header.Size);
				break;
			case BlockType.AbilityData:
				ParseAbilities(stream, header.Size);
				break;
			default:
				Trace.WriteLine(FormattableString.Invariant($"Unknown type [{(int) header.Type:X2}] for ID {header.Id}"));
				break;
		}
	}

	private static ModelObject ParseAbility(BinaryReader reader) {
		dynamic action = new ModelObject();

		action.id = reader.ReadInt16();

		action.type = (AbilityType) reader.ReadByte();
		action.element = reader.ReadByte() & 0x07;
		action.icon_id = reader.ReadInt16();
		action.mp_cost = reader.ReadInt16();
		action.recast_id = reader.ReadInt16();
		action.targets = reader.ReadInt16();
		action.tp_cost = reader.ReadInt16();
		reader.ReadBytes(0x01); // Unknown 0E - 0E, 0x12 for Ready, 0x16 for Ward, 0x17 for Effusion, 0x00 for every other valid ability
		action.monster_level = reader.ReadSByte();
		action.range = reader.ReadSByte() % 0xF;

		// Derived data
		action.prefix = ((AbilityType) action.type).Prefix();

		if (action.tp_cost == -1) {
			action.tp_cost = 0;
		}

		return action;
	}

	private static void ParseAbilities(Stream stream, int length) {
		var recasts = new Dictionary<short, object>();

		var data = new byte[0x30];
		for (var i = 0; i < length / data.Length; ++i) {
			stream.Read(data, 0, data.Length);
			var b2 = data[2];
			var b11 = data[11];
			var b12 = data[12];

			data.Decode();

			data[2] = b2;
			data[11] = b11;
			data[12] = b12;

			using var mstream = new MemoryStream(data);
			using var reader = new BinaryReader(mstream, Encoding.ASCII, true);
			dynamic action = ParseAbility(reader);
			if (action == null) {
				continue;
			}

			model.actions.Add(action);

			// Job ability
			if (action.id >= 0x0200 && action.id < 0x0600) {
				// Add to recast dictionary
				dynamic recast = new ModelObject();
				recast.id = action.recast_id;
				recast.action_id = action.id - 0x200;
				recasts[recast.id] = recast;
			}
		}

		// Remove default value
		recasts.Remove(0);
		model.ability_recasts.AddRange(recasts.Select(kvp => kvp.Value));
	}

	private static void ParseSpells(Stream stream, int length) {
		var data = new byte[0x64];
		for (var i = 0; i < length / data.Length; ++i) {
			stream.Read(data, 0, data.Length);
			var b2 = data[2];
			var b11 = data[11];
			var b12 = data[12];

			data.Decode();

			data[2] = b2;
			data[11] = b11;
			data[12] = b12;

			using var mstream = new MemoryStream(data);
			using var reader = new BinaryReader(mstream, Encoding.ASCII, true);
			dynamic spell = ParseSpell(reader);
			if (spell == null) {
				continue;
			}

			model.spells.Add(spell);
		}
		model.spells.Remove(0);
	}

	private static ModelObject ParseSpell(BinaryReader reader) {
		dynamic spell = new ModelObject();

		spell.id = reader.ReadInt16();
		if (spell.id == 0) {
			return null;
		}

		spell.type = (MagicType) reader.ReadInt16();
		spell.element = reader.ReadByte();
		var padding = reader.ReadByte();                 // Unknown, possibly just padding or element being a short. Always 0x00
		Debug.Assert(padding == 0);
		spell.targets = reader.ReadUInt16();
		spell.skill = reader.ReadInt16();
		spell.mp_cost = reader.ReadInt16();
		spell.cast_time = reader.ReadByte() / 4.0;
		var recast = reader.ReadByte();
		spell.recast = recast < 0xB0 || recast % 0x10 != 0
			? recast / 4.0
			: 300 - (recast / 0x10 - 0xB) * 60;
		spell.levels = new Dictionary<int, int>();
		for (var job = 0; job < 0x18; ++job) {
			var level = reader.ReadInt16();
			if (level != -1) {
				spell.levels[job] = level;
			}
		}

		// Currently the last job slot occasionally mirrors the lowest level that any job learns the spell.
		// May be NPC-related information. This may be removed at some point, if they expand on jobs more
		spell.levels.Remove(0x17);

		// SE changed spell recast times in memory to be indexed by spell ID, not recast ID
		spell._oldrecast = reader.ReadInt16(); // old spell.recast_id
		spell.recast_id = spell.id;

		spell.icon_id_nq = reader.ReadInt16();
		spell.icon_id = reader.ReadInt16();
		spell.requirements = reader.ReadByte();
		var range = reader.ReadByte();
		spell.range = range == 15 ? 0 : range;

		// AoE information?
		spell._aoe_range = reader.ReadByte(); // spell.aoe_range : 11 for uncastable AOE enfeebling. 15 for self target spells
		spell._target_shape = reader.ReadByte(); // spell.target_shape : Target type. 1 for centered on target. 2 for conal. 3 for 
		spell._cursor_behavior = reader.ReadByte(); // spell.cursor_behavior : 1 for self-target non-AoE, 2 for self-target AoE, 5 for self-target AoE that needs an enemy in range, 6 for Geomancy, 8 for Enemy, etc.
		reader.ReadBytes(0x03);

		// Unknown bytes
		spell._unknown12 = reader.ReadByte(); // The first and eighth bits are used. 0, 1, 128, and 129 are observed. They're systematic but I can't see what the covariate is.
		spell._unknown13 = reader.ReadByte(); // 

		// Another requirements field?
		spell._unknown14 = reader.ReadByte(); // 128 if the spell can be stacked with Accession. Seems redundant with the "requirements" field. Takes no other values.
		spell._unknown15 = reader.ReadByte(); // 1 for Manifestation, 2 for Enlightenment, 4 for Embrava, 8 for Meteor, 16 for Geo-spells, 32 for -ra spells, 64 for spells that take all MP (Full Cure and Death)

		//Unknown section
		//reader.ReadBytes(0x0A);
		spell._unknown16 = reader.ReadByte();
		spell._unknown17 = reader.ReadByte();
		spell._unknown18 = reader.ReadByte();
		spell._unknown19 = reader.ReadByte();
		spell._unknown20 = reader.ReadByte();
		spell._unknown21 = reader.ReadByte();
		spell._unknown22 = reader.ReadByte();
		spell._unknown23 = reader.ReadByte();
		spell._unknown24 = reader.ReadByte();
		spell._unknown25 = reader.ReadByte();
		spell._unknown26 = reader.ReadUInt16(); // Always 0x0000
		Debug.Assert((bool) (spell._unknown26 == 0));

		// These next 3 bytes indicate whether a spell is accessible through Gifts for specific jobs.
		reader.ReadByte(); // spells.gift_spell_flags : 0x08 WHM, 0x10 BLM, 0x20 RDM, 0x80 PLD
		reader.ReadByte(); // spells.gift_spell_flags : 0x01 DRK, 0x04 BRD, 0x20 NIN
		reader.ReadByte(); // spells.gift_spell_flags : 0x10 SCH, 0x20 GEO, 0x40 RUN

		var trail = reader.ReadBytes(0x4); // 0x00000000
		foreach (var b in trail) {
			Debug.Assert(b == 0);
		}
		Debug.Assert(reader.ReadSByte() == -1); // 0xFF, last byte

		// Derived data
		spell.prefix = ((MagicType) spell.type).Prefix();

		return spell;
	}

	public static void ParseItems(Stream stream, Stream streamja) {
		var data = new byte[0xC00];
		var dataja = new byte[0xC00];
		var count = (int) (stream.Length / 0xC00);
		for (var i = 0; i < count; i++) {
			stream.Position = streamja.Position = i * 0xC00;

			stream.Read(data, 0, data.Length);
			streamja.Read(dataja, 0, dataja.Length);

			data.RotateRight(5);
			dataja.RotateRight(5);

			using var stringstream = new MemoryStream(data);
			using var stringstreamja = new MemoryStream(dataja);
			using var reader = new BinaryReader(stringstream, Encoding.ASCII, true);
			using var readerja = new BinaryReader(stringstreamja, Encoding.ASCII, true);
			// The english and japanese stream contain the same main item data, so only one stream is needed here
			// The only difference is in the string handling, hence readerja is still needed later
			dynamic item = ParseItem(reader);
			if (item == null) {
				continue;
			}

			// Advance the japanese stream's position to the same as the english one for string parsing
			stringstreamja.Position = stringstream.Position;

			// Parse strings
			if (item.id >= 0xF000 && item.id < 0xF200) {
				item.id -= 0xF000;

				ParseBasicStrings(reader, item, Languages.English);
				ParseBasicStrings(readerja, item, Languages.Japanese);

				model.monstrosity.Add(item);
			} else {
				ParseFullStrings(reader, item, Languages.English);
				ParseFullStrings(readerja, item, Languages.Japanese);

				model.items.Add(item);
			}
		}
	}

	private static ModelObject ParseItem(BinaryReader reader) {
		dynamic item = new ModelObject();

		item.id = reader.ReadUInt16();
		if (item.id == 0x0000) {
			return null;
		}

		reader.ReadBytes(0x02);         // Unknown 02 - 03 (possibly for future expansion of ID)

		if (item.id >= 0xF000 && item.id < 0xF200) {
			ParseMonstrosityItem(reader, item);
		} else if (item.id == 0xFFFF) {
			ParseGilItem(reader, item);
		} else {
			ParseRealItem(reader, item);
		}

		return item;
	}

	private static void ParseRealItem(BinaryReader reader, dynamic item) {
		item.flags = reader.ReadUInt16();
		item.stack = reader.ReadUInt16();
		item.type = reader.ReadUInt16();
		reader.ReadBytes(0x02);     // AH sorting value
		item.targets = reader.ReadUInt16();

		if (item.id <= 0x0FFF) {
			ParseGeneralItem(reader, item);
		} else if (item.id < 0x2000) {
			ParseUsableItem(reader, item);
		} else if (item.id < 0x2200) {
			ParseAutomatonItem(reader, item);
		} else if (item.id < 0x2800) {
			ParseGeneralItem(reader, item);
		} else if (item.id < 0x4000) {
			ParseArmorItem(reader, item);
		} else if (item.id < 0x5A00) {
			ParseWeaponItem(reader, item);
		} else if (item.id < 0x7000) {
			ParseArmorItem(reader, item);
		} else if (item.id < 0x7400) {
			ParseMazeItem(reader, item);
		}
	}

	private static void ParseGilItem(BinaryReader reader, dynamic item) {
		reader.ReadBytes(0x0C);             // Unknown 04 - 0F

		item.category = "Gil";
	}

	private static void ParseGeneralItem(BinaryReader reader, dynamic item) {
		reader.ReadBytes(0x0A);             // Unknown 0E - 17

		item.category = "General";
	}

	private static void ParseWeaponItem(BinaryReader reader, dynamic item) {
		item.level = reader.ReadUInt16();
		item.slots = reader.ReadUInt16();
		item.races = reader.ReadUInt16();
		item.jobs = reader.ReadUInt32();

		reader.ReadBytes(0x04);             // Unknown 18 - 1B
		item.damage = reader.ReadUInt16();
		item.delay = reader.ReadInt16();
		reader.ReadBytes(0x02);             // Weapon DPS
		item.skill = reader.ReadByte();

		reader.ReadBytes(0x05);             // Unknown 23 - 27
											// POLUtils claims that 0x23 is jug size, but seems incorrect
		var max_charges = reader.ReadByte();
		if (max_charges > 0) {
			item.max_charges = max_charges;
		}
		var cast_time = reader.ReadByte() / 4.0;
		if (cast_time > 0) {
			item.cast_time = cast_time;
		}
		var cast_delay = reader.ReadUInt16();
		if (cast_delay > 0) {
			item.cast_delay = cast_delay;
		}
		var recast_delay = reader.ReadUInt32();
		if (recast_delay > 0) {
			item.recast_delay = recast_delay;
		}
		reader.ReadBytes(0x02);             // Unknown 30 - 31
		var item_level = reader.ReadByte();
		if (item_level > 0) {
			item.item_level = item_level;
		}

		reader.ReadBytes(0x05);             // Unknown 33 - 37

		item.category = "Weapon";
	}

	private static void ParseArmorItem(BinaryReader reader, dynamic item) {
		item.level = reader.ReadUInt16();
		item.slots = reader.ReadUInt16();
		item.races = reader.ReadUInt16();
		item.jobs = reader.ReadUInt32();
		var superior_level = reader.ReadByte();
		if (superior_level > 0) {
			item.superior_level = superior_level;
		}
		reader.ReadByte();                  // Unknown 17

		var shield_size = reader.ReadUInt16();
		if (shield_size > 0) {
			item.shield_size = shield_size;
		}
		var max_charges = reader.ReadByte();
		if (max_charges > 0) {
			item.max_charges = max_charges;
		}

		var cast_time = reader.ReadByte() / 4.0;
		if (cast_time > 0) {
			item.cast_time = cast_time;
		}
		var cast_delay = reader.ReadUInt16();
		if (cast_delay > 0) {
			item.cast_delay = cast_delay;
		}
		var recast_delay = reader.ReadUInt32();
		if (recast_delay > 0) {
			item.recast_delay = recast_delay;
		}
		reader.ReadBytes(0x02);             // Unknown 24 - 25
		var item_level = reader.ReadByte();
		if (item_level > 0) {
			item.item_level = item_level;
		}

		reader.ReadBytes(0x05);             // Unknown 27 - 2B

		item.category = "Armor";
	}

	private static void ParseUsableItem(BinaryReader reader, dynamic item) {
		item.cast_time = reader.ReadUInt16() / 4.0;
		reader.ReadBytes(0x08);             // Unknown 10 - 17
		reader.ReadInt32();                 // Not entirely known 18-1B, https://gist.github.com/z16/b1678ffefc4a6e9d52d6

		item.category = "Usable";
	}

	private static void ParseAutomatonItem(BinaryReader reader, dynamic item) {
		reader.ReadBytes(0x0A);             // Unknown 0E - 17

		item.category = "Automaton";
	}

	private static void ParseMazeItem(BinaryReader reader, dynamic item) {
		reader.ReadBytes(0x46);             // Unknown 0E - 53

		item.category = "Maze";
	}

	private static void ParseMonstrosityItem(BinaryReader reader, dynamic item) {
		item.tp_moves = new Dictionary<ushort, sbyte>();
		reader.ReadBytes(0x2C);             // Unknown 04 - 2F
		for (var i = 0x00; i < 0x10; ++i) {
			var move = reader.ReadUInt16();
			var level = reader.ReadSByte();
			if (level != 0 && level != -1 && !item.tp_moves.ContainsKey(move)) {
				item.tp_moves.Add(move, level);
			}

			reader.ReadByte();              // Unknown byte, possibly padding, or level being a short
		}
	}

	private static void ParseBasicStrings(BinaryReader reader, dynamic item, Languages language) {
		// This can potentially be used to disambiguate between languages as well, as their string counts are unique
		reader.ReadUInt32(); // String count

		switch (language) {
			case Languages.English:
				item.en = DecodeEntry(reader, StringIndex.Name);
				break;

			case Languages.Japanese:
				item.ja = DecodeEntry(reader, StringIndex.Name);
				break;
		}
	}

	private static void ParseFullStrings(BinaryReader reader, dynamic item, Languages language) {
		ParseBasicStrings(reader, item, language);

		switch (language) {
			case Languages.English:
				item.art = DecodeEntry(reader, StringIndex.EnglishArticle);
				item.enl = DecodeEntry(reader, StringIndex.EnglishLogSingular);
				item.enlp = DecodeEntry(reader, StringIndex.EnglishLogPlural);
				item.endesc = DecodeEntry(reader, StringIndex.EnglishDescription);
				break;

			case Languages.Japanese:
				item.jal = DecodeEntry(reader, StringIndex.Name);
				item.jadesc = DecodeEntry(reader, StringIndex.JapaneseDescription);
				break;
		}
	}

	private static object DecodeEntry(BinaryReader reader, StringIndex index) {
		var stream = reader.BaseStream;
		var origin = stream.Position;
		reader.ReadBytes(8 * (int) index);
		var dataoffset = reader.ReadInt32();
		var datatype = reader.ReadInt32();
		stream.Position = origin;

		reader.ReadBytes(dataoffset - 4);

		switch (datatype) {
			case 0:
				reader.ReadBytes(0x1C);
				var dataorigin = stream.Position;

				while (stream.Position != stream.Length && reader.ReadByte() != 0) {
				}

				var length = (int) (stream.Position - dataorigin) - 1;
				stream.Position = dataorigin;

				var res = ShiftJISFF11Encoding.ShiftJISFF11.GetString(reader.ReadBytes(length), 0, length);
				stream.Position = origin;
				return res;

			case 1:
				var value = reader.ReadInt32();
				stream.Position = origin;
				return value;
		}

		return null;
	}
}
