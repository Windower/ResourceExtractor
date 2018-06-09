// <copyright file="LuaAttribute.cs" company="Windower Team">
// Copyright © 2013-2018 Windower Team
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

using System.Linq;

namespace ResourceExtractor.Serializers.Lua
{
    using System;
    using System.Collections;

    internal class LuaAttribute
    {
        public LuaAttribute(string key, object value)
        {
            Key = key;
            Value = value;
        }

        private string Key { get; }

        private object Value { get; }

        public override string ToString()
        {
            return FormattableString.Invariant($"{MakeKey(Key)}={MakeValue(Value)}");
        }

        private static string MakeKey(object key)
        {
            return key is string ? FormattableString.Invariant($"{key}") : FormattableString.Invariant($"[{key}]");
        }

        private static string MakeValue(object value)
        {
            if (value is string || value is Enum)
            {
                return "\"" + value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
            }

            if (value is IDictionary vdict)
            {
                var lines = vdict.Keys.Cast<object>().Select(key => $"{MakeKey(key)}={MakeValue(vdict[key])}").ToList();
                return $"{{{string.Join(",", lines)}}}";
            }

            if (value is IEnumerable venum)
            {
                return $"{{{string.Join(",", venum.Cast<object>().Select(MakeValue))}}}";
            }

            return FormattableString.Invariant($"{value}").ToLower();
        }
    }
}
