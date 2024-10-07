using System;
using System.Collections;
using System.Linq;

namespace ResourceExtractor.Serializers;

internal class LuaAttribute(string key, object value) {
	public override string ToString() =>
		FormattableString.Invariant($"{MakeKey(key)}={MakeValue(value)}");

	private static string MakeKey(object key) =>
		key is string
			? FormattableString.Invariant($"{key}")
			: FormattableString.Invariant($"[{key}]");

	public static string MakeValue(object value) =>
		value switch {
			string or Enum => $"\"{value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n")}\"",
			IDictionary dictionary => $"{{{String.Join(",", dictionary.Keys.Cast<object>().Select(key => $"{MakeKey(key)}={MakeValue(dictionary[key])}"))}}}",
			IEnumerable enumerable => $"{{{String.Join(",", enumerable.Cast<object>().Select(MakeValue))}}}",
			_ => FormattableString.Invariant($"{value}").ToLower(),
		};
}
