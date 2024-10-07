using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResourceExtractor; 

internal static class Analyzer {
	internal static void Analyze(IEnumerable<KeyValuePair<string, dynamic>> model) {
		const string rootPath = "analysis";

		Program.DisplayMessage("Starting analysis...");

		Directory.CreateDirectory(rootPath);
		foreach (var pair in model) {
			var namePath = Path.Combine(rootPath, pair.Key);
			Directory.CreateDirectory(namePath);
			foreach (var property in Group((IEnumerable<dynamic>) pair.Value, pair.Key)) {
				var propertyName = property.Key[1..];
				using var file = File.Open(Path.Combine(namePath, $"{propertyName}.lua"), FileMode.Create);
				using var writer = new StreamWriter(file);
				writer.WriteLine("return {");

				var attributes = FindCommonAttributes(property.Value);
				var commonAttributeNames = attributes.Values.Select(value => value.Keys).Aggregate((current, obj) => new HashSet<string>(current.Intersect(obj))).Distinct().OrderBy(str => str).ToList();

				foreach (var bucket in property.Value.OrderBy(bucket => bucket.Key)) {
					var set = bucket.Value;
					var comment = "";
					var names = set.Select(obj => MakeValue(obj.en));
					if (set.Count > 1) {
						var localAttributes = (IDictionary<string, dynamic>) attributes[bucket.Key];
						if (localAttributes.Count > 0) {
							comment = " --";
							var localCommonAttributeNames = commonAttributeNames.ToList();
							if (localCommonAttributeNames.Count > 1) {
								comment += $" {String.Join(", ", localCommonAttributeNames.Where(name => name != propertyName).Select(name => $"{name} = {MakeValue(localAttributes[name])}"))}";
							}
							comment += $"  ({String.Join(", ", localAttributes.Where(attr => !localCommonAttributeNames.Contains(attr.Key)).OrderBy(attr => attr.Key).Select(attr => $"{attr.Key} = {MakeValue(attr.Value)}"))})";
						} else {
							comment = " -- No common values";
						}
					}

					if (set.Count <= 5) {
						writer.WriteLine($"    [{MakeValue(bucket.Key)}] = {{{String.Join(", ", names)}}},{comment}");
					} else {
						writer.WriteLine($"    [{MakeValue(bucket.Key)}] = {{");
						foreach (var name in names) {
							writer.WriteLine($"        {name},");
						}
						writer.WriteLine($"    }},{comment}");
					}
				}

				writer.WriteLine("}");
			}
		}

		static string MakeValue(dynamic value) {
			if (value is string || value is Enum) {
				return "\"" + value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			}

			return FormattableString.Invariant($"{value}");
		}
	}

	private static IDictionary<string, IDictionary<dynamic, ISet<dynamic>>> Group(IEnumerable<dynamic> dict, string resName) {
		var properties = new Dictionary<string, IDictionary<dynamic, ISet<dynamic>>>();
		foreach (var obj in dict) {
			foreach (var attribute in obj) {
				if (!Program.IsValidObject(resName, obj)) {
					continue;
				}

				var name = (string) attribute.Key;
				if (!name.StartsWith('_')) {
					continue;
				}

				if (!properties.TryGetValue(name, out var property)) {
					property = new Dictionary<dynamic, ISet<dynamic>>();
					properties[name] = property;
				}

				var value = attribute.Value;
				if (!property.ContainsKey(value)) {
					property[value] = new HashSet<dynamic>();
				}

				property[value].Add(obj);
			}
		}

		return properties;
	}

	private static IDictionary<dynamic, IDictionary<string, dynamic>> FindCommonAttributes(IDictionary<dynamic, ISet<dynamic>> buckets) {
		return buckets.ToDictionary(pair => pair.Key, pair => FindCommonAttributesForSet(pair.Value));

		static IDictionary<string, dynamic> FindCommonAttributesForSet(ISet<dynamic> objects) {
			if (objects.Count <= 1) {
				return new Dictionary<string, dynamic>();
			}

			var attributes = objects.Aggregate((current, obj) => Enumerable.Intersect(current, (IEnumerable<KeyValuePair<string, dynamic>>) obj));

			return ((IEnumerable<KeyValuePair<string, dynamic>>) attributes).ToDictionary(pair => pair.Key.StartsWith('_') ? pair.Key[1..] : pair.Key, pair => pair.Value);
		}
	}
}
