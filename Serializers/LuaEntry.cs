using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExtractor.Serializers;

internal class LuaEntry {
	public int ID { get; }
	public HashSet<string> Keys { get; } = [];

	public LuaEntry(dynamic obj) {
		ID = obj.id;

		foreach (var key in FixedKeys.Where(key => obj.ContainsKey(key))) {
			Attributes.Add(new LuaAttribute(key, obj[key]));
			Keys.Add(key);
		}

		// TODO: Prettier
		var otherKeys = new List<string>();
		foreach (var pair in obj) {
			if (pair.Key.StartsWith("_")) {
				continue;
			}
			otherKeys.Add(pair.Key);
		}
		foreach (var key in otherKeys.Where(key => !FixedKeys.Contains(key)).OrderBy(key => key)) {
			Attributes.Add(new LuaAttribute(key, obj[key]));
			Keys.Add(key);
		}
	}

	public override string ToString() =>
		$"{{{String.Join(",", Attributes.Select(attr => attr.ToString()))}}}";

	private List<LuaAttribute> Attributes { get; } = [];

	private static readonly string[] FixedKeys = ["id", "en", "ja", "enl", "jal"];
}
