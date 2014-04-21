// <copyright file="Container.cs" company="Windower Team">
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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    class LuaAttribute
    {
        private string Key { get; set; }
        private object Value { get; set; }

        public LuaAttribute(string Key, object Value)
        {
            this.Key = Key;
            this.Value = Value;
        }

        private static string MakeKey(object Key)
        {
            return String.Format(
                Key is string ? "{0}" :
                "[{0}]",
                Key);
        }
        private static string MakeValue(object Value)
        {
            if (Value is String || Value is Enum)
            {
                return "\"" + Value.ToString().Replace("\"", "\\\"") + "\"";
            }

            var vdict = Value as IDictionary;
            if (vdict != null)
            {
                string str = "{";
                bool first = true;
                foreach (var v in vdict.Keys)
                {
                    str += first ? "" : ",";
                    str += MakeKey(v);
                    str += "=";
                    str += MakeValue(vdict[v]);
                    first = false;
                }
                str += "}";
                return str;
            }

            var venum = Value as IEnumerable;
            if (venum != null)
            {
                string str = "{";
                bool first = true;
                foreach (var v in venum)
                {
                    str += first ? "" : ",";
                    str += v.ToString();
                    first = false;
                }
                str += "}";
                return str;
            }

            return Value.ToString();
        }
        public override string ToString()
        {
            return String.Format("{0}={1}", MakeKey(Key), MakeValue(Value));
        }
    }
}
