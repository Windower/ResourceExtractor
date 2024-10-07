using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ResourceExtractor; 

internal static class Fixes {
	public static void Apply(object obj, string path = "fixes.xml") {
		Apply(obj, XDocument.Load(path));
	}

	public static void Apply(object obj, Stream stream) {
		Apply(obj, XDocument.Load(stream));
	}

	public static void Apply(object obj, XDocument document) {
		Apply(obj, document.Root);
	}

	private static void Apply(object obj, XElement element) {
		if (IsList(obj.GetType())) {
			foreach (var e in element.Elements("update")) {
				var key = (int) e.Attribute("key");
				var value = (string) e.Attribute("value");
				var type = (string) e.Attribute("type");
				if (value != null) {
					SetIndex(obj, key, Convert(value, type));
				} else if (e.HasElements) {
					var current = GetIndex(obj, key);

					if (type == "list") {
						if (current == null || !IsList(current.GetType())) {
							current = new List<object>();
							SetIndex(obj, key, current);
						}
					} else if (current == null) {
						current = new ModelObject();
						SetIndex(obj, key, current);
					}

					Apply(current, e);
				}
			}
		} else {
			foreach (var e in element.Elements("update")) {
				var key = (string) e.Attribute("key");
				if (key != null) {
					var value = (string) e.Attribute("value");
					var type = (string) e.Attribute("type");
					if (value != null) {
						SetDynamic(obj, key, Convert(value, type));
					} else if (e.HasElements) {
						var current = GetDynamic(obj, key);

						if (type == "list") {
							if (current == null || !IsList(current.GetType())) {
								current = new List<object>();
								SetDynamic(obj, key, current);
							}
						} else if (current == null) {
							current = new ModelObject();
							SetDynamic(obj, key, current);
						}

						Apply(current, e);
					}
				}
			}
		}
	}

	private static bool IsList(Type type) =>
		type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

	private static object GetIndex(object obj, int key) {
		dynamic dyn = obj;
		try {
			return dyn[key];
		} catch (ArgumentOutOfRangeException) {
			return null;
		}
	}

	private static void SetIndex(object obj, int key, object value) {
		dynamic dyn = obj;
		if (key < dyn.Count) {
			dyn[key] = value;
		} else {
			dyn.Insert(key, value);
		}
	}

	private static object GetDynamic(object obj, string key) {
		try {
			var binder = Binder.GetMember(CSharpBinderFlags.None, key, obj.GetType(), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
			var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);
			return callsite.Target(callsite, obj);
		} catch (RuntimeBinderException) {
			return null;
		}
	}

	private static void SetDynamic(object obj, string key, object value) {
		var binder = Binder.SetMember(CSharpBinderFlags.None, key, obj.GetType(), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
		var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);
		callsite.Target(callsite, obj, value);
	}

	private static object Convert(string value, string type) {
		return type switch {
			null or "string" => value,
			"bool" => Boolean.Parse(value),
			"sbyte" => SByte.Parse(value, CultureInfo.InvariantCulture),
			"short" => Int16.Parse(value, CultureInfo.InvariantCulture),
			"int" => Int32.Parse(value, CultureInfo.InvariantCulture),
			"long" => Int64.Parse(value, CultureInfo.InvariantCulture),
			"byte" => Byte.Parse(value, CultureInfo.InvariantCulture),
			"ushort" => UInt16.Parse(value, CultureInfo.InvariantCulture),
			"uint" => UInt32.Parse(value, CultureInfo.InvariantCulture),
			"ulong" => UInt64.Parse(value, CultureInfo.InvariantCulture),
			"float" => Single.Parse(value, CultureInfo.InvariantCulture),
			"double" => Double.Parse(value, CultureInfo.InvariantCulture),
			"decimal" => Decimal.Parse(value, CultureInfo.InvariantCulture),
			"char" => Char.Parse(value),
			_ => null,
		};
	}
}

