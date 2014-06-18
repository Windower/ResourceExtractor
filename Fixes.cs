// <copyright file="ExtensionMethods.cs" company="Windower Team">
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
    using Microsoft.CSharp.RuntimeBinder;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Xml.Linq;

    internal static class Fixes
    {
        public static void Apply(object obj, string path = "fixes.xml")
        {
            Apply(obj, XDocument.Load(path));
        }

        public static void Apply(object obj, Stream stream)
        {
            Apply(obj, XDocument.Load(stream));
        }

        public static void Apply(object obj, XDocument document)
        {
            Apply(obj, document.Root);
        }

        private static void Apply(object obj, XElement element)
        {
            if (IsList(obj.GetType()))
            {
                //// var count = (int)obj.GetType().GetProperty("Count").GetValue(obj);

                foreach (var e in element.Elements("update"))
                {
                    var key = (int)e.Attribute("key");
                    var value = (string)e.Attribute("value");
                    var type = (string)e.Attribute("type");
                    if (value != null)
                    {
                        SetIndex(obj, key, Convert(value, type));
                    }
                    else if (e.HasElements)
                    {
                        var current = GetIndex(obj, key);

                        if (type == "list")
                        {
                            if (current == null || !IsList(current.GetType()))
                            {
                                current = new List<object>();
                                SetIndex(obj, key, current);
                            }
                        }
                        else if (current == null)
                        {
                            current = new ModelObject();
                            SetIndex(obj, key, current);
                        }

                        Apply(current, e);
                    }
                }
            }
            else
            {
                foreach (var e in element.Elements("update"))
                {
                    var key = (string)e.Attribute("key");
                    if (key != null)
                    {
                        var value = (string)e.Attribute("value");
                        var type = (string)e.Attribute("type");
                        if (value != null)
                        {
                            SetDynamic(obj, key, Convert(value, type));
                        }
                        else if (e.HasElements)
                        {
                            var current = GetDynamic(obj, key);

                            if (type == "list")
                            {
                                if (current == null || !IsList(current.GetType()))
                                {
                                    current = new List<object>();
                                    SetDynamic(obj, key, current);
                                }
                            }
                            else if (current == null)
                            {
                                current = new ModelObject();
                                SetDynamic(obj, key, current);
                            }

                            Apply(current, e);
                        }
                    }
                }
            }
        }

        private static bool IsList(Type type)
        {
            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return true;
                }
            }

            return false;
        }

        private static object GetIndex(object obj, int key)
        {
            dynamic dyn = obj;
            try
            {
                return dyn[key];
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static void SetIndex(object obj, int key, object value)
        {
            dynamic dyn = obj;
            if (key < dyn.Count)
            {
                dyn[key] = value;
            }
            else
            {
                dyn.Insert(key, value);
            }
        }

        private static object GetDynamic(object obj, string key)
        {
            try
            {
                var binder = Binder.GetMember(CSharpBinderFlags.None, key, obj.GetType(), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
                var callsite = CallSite<Func<CallSite, object, object>>.Create(binder);
                return callsite.Target(callsite, obj);
            }
            catch (RuntimeBinderException)
            {
                return null;
            }
        }

        private static void SetDynamic(object obj, string key, object value)
        {
            var binder = Binder.SetMember(CSharpBinderFlags.None, key, obj.GetType(), new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null), CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            var callsite = CallSite<Func<CallSite, object, object, object>>.Create(binder);
            callsite.Target(callsite, obj, value);
        }

        private static object Convert(string value, string type)
        {
            switch (type)
            {
                case null:
                case "string":
                    return value;
                case "bool":
                    return bool.Parse(value);
                case "sbyte":
                    return sbyte.Parse(value, CultureInfo.InvariantCulture);
                case "short":
                    return short.Parse(value, CultureInfo.InvariantCulture);
                case "int":
                    return int.Parse(value, CultureInfo.InvariantCulture);
                case "long":
                    return long.Parse(value, CultureInfo.InvariantCulture);
                case "byte":
                    return byte.Parse(value, CultureInfo.InvariantCulture);
                case "ushort":
                    return ushort.Parse(value, CultureInfo.InvariantCulture);
                case "uint":
                    return uint.Parse(value, CultureInfo.InvariantCulture);
                case "ulong":
                    return ulong.Parse(value, CultureInfo.InvariantCulture);
                case "float":
                    return float.Parse(value, CultureInfo.InvariantCulture);
                case "double":
                    return double.Parse(value, CultureInfo.InvariantCulture);
                case "decimal":
                    return decimal.Parse(value, CultureInfo.InvariantCulture);
                case "char":
                    return char.Parse(value);
            }
            return null;
        }
    }
}

