using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResourceExtractor; 

internal static class Analyzer {
	internal static void Analyze(IEnumerable<KeyValuePair<string, List<ModelObject>>> data) {
		const string rootPath = "analysis";

		Program.DisplayMessage("Starting analysis...");

		Directory.CreateDirectory(rootPath);
		foreach (var pair in data) {
			var namePath = Path.Combine(rootPath, pair.Key);
			Directory.CreateDirectory(namePath);
			foreach (var property in Group(pair.Value, pair.Key)) {
				var propertyName = property.Key[1..];
				using var file = File.Open(Path.Combine(namePath, $"{propertyName}.lua"), FileMode.Create);
				using var writer = new StreamWriter(file);
				writer.WriteLine("return {");

				var attributes = FindCommonAttributes(property.Value);
				var commonAttributeNames = attributes.Values.Select(value => value.Keys).Aggregate((current, obj) => new HashSet<string>(current.Intersect(obj))).Distinct().OrderBy(str => str).ToList();

				foreach (var bucket in property.Value.OrderBy(bucket => bucket.Key)) {
					var set = bucket.Value;
					var comment = "";
					var names = set.Select(obj => MakeValue(obj["en"]));
					if (set.Count > 1) {
						var localAttributes = attributes[bucket.Key];
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

		static string MakeValue(object value) {
			if (value is string || value is Enum) {
				return "\"" + value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
			}

			return FormattableString.Invariant($"{value}");
		}
	}

	private static IDictionary<string, IDictionary<object, ISet<ModelObject>>> Group(List<ModelObject> resources, string resName) {
		var properties = new Dictionary<string, IDictionary<object, ISet<ModelObject>>>();
		foreach (var resource in resources) {
			foreach (var attribute in resource) {
				if (!Program.IsValidObject(resName, resource)) {
					continue;
				}

				var name = attribute.Key;
				if (!name.StartsWith('_')) {
					continue;
				}

				if (!properties.TryGetValue(name, out var property)) {
					property = new Dictionary<object, ISet<ModelObject>>();
					properties[name] = property;
				}

				var value = attribute.Value;
				if (!property.TryGetValue(value, out var group)) {
					group = new HashSet<ModelObject>();
					property[value] = group;
				}

				group.Add(resource);
			}
		}

		return properties;
	}

	private static IDictionary<object, IDictionary<string, object>> FindCommonAttributes(IDictionary<object, ISet<ModelObject>> buckets) {
		return buckets.ToDictionary(pair => pair.Key, pair => FindCommonAttributesForSet(pair.Value));

		static IDictionary<string, object> FindCommonAttributesForSet(ISet<ModelObject> objects) {
			if (objects.Count <= 1) {
				return new Dictionary<string, object>();
			}

			var attributes = objects.Cast<IEnumerable<KeyValuePair<string, object>>>().Aggregate((current, obj) => Enumerable.Intersect(current, obj));

			return attributes.ToDictionary(pair => pair.Key.StartsWith('_') ? pair.Key[1..] : pair.Key, pair => pair.Value);
		}
	}
}
