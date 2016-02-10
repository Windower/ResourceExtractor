// <copyright file="LuaElement.cs" company="Windower Team">
// Copyright © 2013-2014 Windower Team
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

namespace ResourceExtractor.Serializers.Lua
{
    using System.Collections.Generic;
    using System.Linq;

    internal class LuaElement
    {
        private static readonly List<string> FixedKeys = new List<string> { "id", "en", "ja", "enl", "jal", };

        public LuaElement(dynamic obj)
        {
            Attributes = new List<LuaAttribute>();

            Keys = new HashSet<string>();

            ID = obj.id;

            foreach (var key in FixedKeys.Where(key => obj.ContainsKey(key)))
            {
                Attributes.Add(new LuaAttribute(key, obj[key]));
                Keys.Add(key);
            }

            // TODO: Prettier
            var otherKeys = new List<string>();
            foreach (var pair in obj)
            {
                otherKeys.Add(pair.Key);
            }
            foreach (var key in otherKeys.Where(key => !FixedKeys.Contains(key)).OrderBy(key => key))
            {
                Attributes.Add(new LuaAttribute(key, obj[key]));
                Keys.Add(key);
            }
        }

        public HashSet<string> Keys { get; private set; }

        public int ID { get; private set; }

        private List<LuaAttribute> Attributes { get; set; }

        public override string ToString()
        {
            return "{" + string.Join(",", Attributes.Select(attr => attr.ToString())) + "}";
        }
    }
}
