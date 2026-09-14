using System;
using System.Collections.Generic;
using System.Linq;

namespace ResourceExtractor.Serializers;

internal class LuaEntry {
	public int ID { get; }
	public HashSet<string> Keys { get; } = ["id"];

	public LuaEntry(ModelObject obj) {
		ID = obj.Id;

		Attributes.Add(new("id", obj.Id));

		foreach (var key in FixedKeys.Where(obj.ContainsKey)) {
			Attributes.Add(new LuaAttribute(key, obj[key]));
			Keys.Add(key);
		}

		// TODO: Prettier
		var otherKeys = new List<string>();
		foreach (var pair in obj) {
			if (pair.Key.StartsWith('_')) {
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

	private static readonly string[] FixedKeys = ["en", "ja", "enl", "jal"];
}
