// <copyright file="LuaFormatter.cs" company="Windower Team">
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

namespace ResourceExtractor.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.RegularExpressions;

    public class LuaFormatter : IFormatter
    {
        private static Encoding encoding = new UTF8Encoding();

        private static Regex identifier = new Regex("^[A-Z_a-z][0-9A-Z_a-z]*$");
        private static string[] keywords =
        {
            "and", "break", "do", "else", "elseif", "end", "false", "for",
            "function", "if", "in", "local", "nil", "not", "or", "repeat",
            "return", "then", "true", "until", "while"
        };

        public LuaFormatter()
        {
            Context = new StreamingContext(StreamingContextStates.File);
            KeyPriority = new List<string>()
            {
                "id", "en", "ja", "enl", "jal"
            };
        }

        public SerializationBinder Binder { get; set; }

        public StreamingContext Context { get; set; }

        public ISurrogateSelector SurrogateSelector { get; set; }

        public IList<string> KeyPriority { get; private set; }

        public object Deserialize(Stream serializationStream)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Stream serializationStream, object graph)
        {
            if (serializationStream == null)
            {
                throw new ArgumentNullException("serializationStream");
            }

            using (StreamWriter writer = new StreamWriter(serializationStream, encoding, 1024, true))
            {
                Serialize(writer, graph);
            }
        }

        private static bool IsNumeric(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                default:
                    return false;

                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
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

        private void Serialize(TextWriter writer, object graph)
        {
            if (graph == null)
            {
                writer.Write("nil");
            }

            if (!graph.GetType().IsArray && Attribute.GetCustomAttribute(graph.GetType(), typeof(SerializableAttribute)) == null)
            {
                throw new SerializationException();
            }

            switch (Type.GetTypeCode(graph.GetType()))
            {
                case TypeCode.Boolean:
                    SerializeBoolean(writer, graph);
                    break;

                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    SerializeNumber(writer, graph);
                    break;

                case TypeCode.Char:
                case TypeCode.String:
                    SerializeString(writer, graph);
                    break;

                default:
                    SerializeTable(writer, graph);
                    break;
            }
        }

        private void SerializeBoolean(TextWriter writer, object value)
        {
            writer.Write((bool)value ? "true" : "false");
        }

        private void SerializeNumber(TextWriter writer, object value)
        {
            writer.Write(value);
        }

        private void SerializeString(TextWriter writer, object value)
        {
            var temp = new StringBuilder().Append(value);
            temp.Replace("\\", "\\\\");
            temp.Replace("'", "\\'");
            temp.Replace("\r\n", "\\n");
            temp.Replace("\n", "\\n");
            temp.Replace("\r", "\\n");
            temp.Replace("\t", "\\t");
            for (int i = 0; i < temp.Length; i++)
            {
                char c = temp[i];
                if (c < 0x20)
                {
                    temp[i] = '\\';
                    temp.Insert(i + 1, ((int)c).ToString("000", CultureInfo.InvariantCulture));
                    i += 3;
                }
            }
            writer.Write(temp.Insert(0, '\'').Append('\''));
        }

        private void SerializeTable(TextWriter writer, object value)
        {
            var pairs = new List<KeyValuePair<object, object>>();

            if (IsList(value.GetType()))
            {
                int index = 0;
                foreach (object v in (IEnumerable)value)
                {
                    pairs.Add(new KeyValuePair<object, object>(index++, v));
                }
            }
            else
            {
                var serializable = value as ISerializable;
                if (serializable != null)
                {
                    SerializationInfo info = new SerializationInfo(value.GetType(), new FormatterConverter());
                    serializable.GetObjectData(info, Context);
                    foreach (SerializationEntry entry in info)
                    {
                        pairs.Add(new KeyValuePair<object, object>(entry.Name, entry.Value));
                    }
                }
                else
                {
                    foreach (var member in value.GetType().GetMembers())
                    {
                        if (Attribute.GetCustomAttribute(member, typeof(NonSerializedAttribute)) == null)
                        {
                            switch (member.MemberType)
                            {
                                case MemberTypes.Property:
                                    var property = (PropertyInfo)member;
                                    if (property.CanRead && !property.GetGetMethod().IsStatic && property.GetIndexParameters().Length == 0)
                                    {
                                        pairs.Add(new KeyValuePair<object, object>(property.Name, property.GetValue(value, null)));
                                    }

                                    break;

                                case MemberTypes.Field:
                                    var field = (FieldInfo)member;
                                    if (!field.IsStatic)
                                    {
                                        pairs.Add(new KeyValuePair<object, object>(field.Name, field.GetValue(value)));
                                    }

                                    break;
                            }
                        }
                    }
                }
            }

            pairs.Sort(IndexComparer);

            writer.Write('{');

            bool first = true;
            foreach (var pair in pairs)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    if (!first)
                    {
                        writer.Write(',');
                    }
                    first = false;

                    SerializeIndex(writer, pair.Key);
                    writer.Write('=');
                    Serialize(writer, pair.Value);
                }
            }

            writer.Write('}');
        }

        private void SerializeIndex(TextWriter writer, object index)
        {
            string str = index as string;
            if (str != null && identifier.IsMatch(str) && !keywords.Contains(str))
            {
                writer.Write(str);
            }
            else
            {
                writer.Write('[');
                Serialize(writer, index);
                writer.Write(']');
            }
        }

        private int IndexComparer(KeyValuePair<object, object> x, KeyValuePair<object, object> y)
        {
            object a = x.Key;
            object b = y.Key;

            if (IsNumeric(a.GetType()))
            {
                if (IsNumeric(b.GetType()))
                {
                    var tempa = Convert.ToDouble(a, CultureInfo.InvariantCulture);
                    var tempb = Convert.ToDouble(b, CultureInfo.InvariantCulture);
                    return tempa < tempb ? -1 : tempa > tempb ? 1 : 0;
                }

                return KeyPriority.IndexOf(b.ToString()) < 0 ? -1 : 1;
            }
            else if (IsNumeric(b.GetType()))
            {
                return KeyPriority.IndexOf(a.ToString()) < 0 ? 1 : -1;
            }

            var stringa = a.ToString();
            var stringb = b.ToString();

            var indexa = KeyPriority.IndexOf(stringa);
            var indexb = KeyPriority.IndexOf(stringb);
            if (indexa >= 0)
            {
                if (indexb >= 0)
                {
                    return indexa < indexb ? -1 : indexa > indexb ? 1 : 0;
                }

                return -1;
            }
            else if (indexb >= 0)
            {
                return 1;
            }

            return string.Compare(stringa, stringb, StringComparison.OrdinalIgnoreCase);
        }
    }
}
