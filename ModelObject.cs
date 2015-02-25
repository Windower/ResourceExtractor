// <copyright file="ModelObject.cs" company="Windower Team">
// Copyright © 2014 Windower Team
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
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Runtime.Serialization;

    [Serializable]
    internal class ModelObject : DynamicObject, ISerializable, IEnumerable<KeyValuePair<string, object>>
    {
        private IDictionary<string, object> map = new Dictionary<string, object>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return map.TryGetValue(binder.Name, out result);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (value != null)
            {
                map[binder.Name] = value;
            }
            else
            {
                map.Remove(binder.Name);
            }
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            if (indexes.Length == 1)
            {
                var name = indexes[0] as string;
                if (name != null)
                {
                    return map.TryGetValue(name, out result);
                }
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length == 1)
            {
                var name = indexes[0] as string;
                if (name != null)
                {
                    if (value != null)
                    {
                        map[name] = value;
                    }
                    else
                    {
                        map.Remove(name);
                    }
                    return true;
                }
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public override bool TryDeleteMember(DeleteMemberBinder binder)
        {
            map.Remove(binder.Name);
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            if (indexes.Length == 1)
            {
                var name = indexes[0] as string;
                if (name != null)
                {
                    map.Remove(name);
                    return true;
                }
            }

            return false;
        }

        public void Add(string key, object value)
        {
            map.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            map.Add(item);
        }

        public void Remove(string key)
        {
            map.Remove(key);
        }

        public void Remove(KeyValuePair<string, object> item)
        {
            map.Remove(item);
        }

        public void Merge(ModelObject other)
        {
            foreach (var pair in other)
            {
                map[pair.Key] = pair.Value;
            }
        }

        public bool ContainsKey(string key)
        {
            return map.ContainsKey(key);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            foreach (var pair in map)
            {
                info.AddValue(pair.Key, pair.Value);
            }
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.GetEnumerator();
        }
    }
}

