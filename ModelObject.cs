using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace ResourceExtractor; 

internal class ModelObject : DynamicObject, IEnumerable<KeyValuePair<string, object>> {
	private readonly IDictionary<string, object> map = new Dictionary<string, object>();

	public override bool TryGetMember(GetMemberBinder binder, out object result) =>
		map.TryGetValue(binder.Name, out result);

	public override bool TrySetMember(SetMemberBinder binder, object value) {
		if (value != null) {
			map[binder.Name] = value;
		} else {
			map.Remove(binder.Name);
		}
		return true;
	}

	public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result) {
		result = null;
		return indexes.Length == 1 && indexes[0] is string name && map.TryGetValue(name, out result);
	}

	public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value) {
		if (indexes.Length != 1 || indexes[0] is not string name) {
			return false;
		}

		if (value != null) {
			map[name] = value;
		} else {
			map.Remove(name);
		}
		return true;
	}

	public override bool TryDeleteMember(DeleteMemberBinder binder) {
		map.Remove(binder.Name);
		return true;
	}

	public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes) {
		if (indexes.Length != 1 || indexes[0] is not string name) {
			return false;
		}

		map.Remove(name);
		return true;
	}

	public void Add(string key, object value) =>
		map.Add(key, value);

	public void Add(KeyValuePair<string, object> item) =>
		map.Add(item);

	public void Remove(string key) =>
		map.Remove(key);

	public void Remove(KeyValuePair<string, object> item) =>
		map.Remove(item);

	public void Merge(ModelObject other) {
		foreach (var pair in other) {
			map[pair.Key] = pair.Value;
		}
	}
	public bool ContainsKey(string key) =>
		map.ContainsKey(key);

	IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() =>
		map.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() =>
		map.GetEnumerator();
}

