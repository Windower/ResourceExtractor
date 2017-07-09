// <copyright file="Analyzer.cs" company="Windower Team">
// Copyright © 2017 Windower Team
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResourceExtractor
{
    internal static class Analyzer
    {
        internal static void Analyze(IEnumerable<KeyValuePair<string, dynamic>> model)
        {
            var rootPath = "analysis";
            Directory.CreateDirectory(rootPath);
            foreach (var pair in model)
            {
                var namePath = Path.Combine(rootPath, pair.Key);
                Directory.CreateDirectory(namePath);
                foreach (var property in Extract((IEnumerable<dynamic>) pair.Value, pair.Key))
                {
                    using (var file = File.Open(Path.Combine(namePath, $"{property.Key.Substring(1)}.lua"), FileMode.Create))
                    using (var writer = new StreamWriter(file))
                    {
                        writer.WriteLine("return {");

                        foreach (var bucket in property.Value.OrderBy(bucket => bucket.Key))
                        {
                            var names = bucket.Value.Select(obj => MakeValue(obj.en));
                            if (bucket.Value.Count <= 5)
                            {
                                writer.WriteLine($"    [{MakeValue(bucket.Key)}] = {{{string.Join(", ", names)}}}");
                            }
                            else
                            {
                                writer.WriteLine($"    [{MakeValue(bucket.Key)}] = {{");
                                foreach (var name in names)
                                {
                                    writer.WriteLine($"        {name},");
                                }
                                writer.WriteLine("    },");
                            }
                        }

                        writer.WriteLine("}");
                    }
                }
            }
        }

        internal static IDictionary<string, IDictionary<dynamic, ISet<dynamic>>> Extract(IEnumerable<dynamic> dict, string resName)
        {
            var properties = new Dictionary<string, IDictionary<dynamic, ISet<dynamic>>>();
            foreach (var obj in dict)
            {
                foreach (var attribute in obj)
                {
                    if (!Program.IsValidObject(resName, obj))
                    {
                        continue;
                    }

                    var name = (string) attribute.Key;
                    if (!name.StartsWith("_"))
                    {
                        continue;
                    }

                    if (!properties.ContainsKey(name))
                    {
                        properties[name] = new Dictionary<dynamic, ISet<dynamic>>();
                    }

                    var property = properties[name];

                    var value = attribute.Value;
                    if (!property.ContainsKey(value))
                    {
                        property[value] = new HashSet<dynamic>();
                    }

                    property[value].Add(obj);
                }
            }

            return properties;
        }

        private static string MakeValue(dynamic value)
        {
            if (value is string || value is Enum)
            {
                return "\"" + value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
            }

            return FormattableString.Invariant($"{value}");
        }
    }
}
