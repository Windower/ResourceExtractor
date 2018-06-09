// <copyright file="ExtensionMethods.cs" company="Windower Team">
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

namespace ResourceExtractor
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Xml.Linq;

    internal static class ExtensionMethods
    {
        public static void RotateRight(this byte[] data, int count)
        {
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                data[i] = (byte)(b >> count | b << (8 - count));
            }
        }

        public static void Decode(this byte[] data)
        {
            if (data.Length < 13)
            {
                return;
            }

            int key = 0;

            int count = Math.Abs(CountBits(data[2]) - CountBits(data[11]) + CountBits(data[12]));
            switch (count % 5)
            {
                case 0: key = 7; break;
                case 1: key = 1; break;
                case 2: key = 6; break;
                case 3: key = 2; break;
                case 4: key = 5; break;
            }

            data.RotateRight(key);
        }

        public static T Read<T>(this Stream stream)
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] data = new byte[size];
            stream.Read(data, 0, data.Length);

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);
                return (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        public static T Read<T>(this Stream stream, uint offset)
        {
            stream.Position = offset;
            return Read<T>(stream);
        }

        public static T[] ReadArray<T>(this Stream stream, int count, uint offset)
        {
            stream.Position = offset;
            return stream.ReadArray<T>(count);
        }

        public static T[] ReadArray<T>(this Stream stream, int count)
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] data = new byte[size * count];
            stream.Read(data, 0, data.Length);

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);

                T[] result = new T[count];

                for (int i = 0; i < count; i++)
                {
                    result[i] = (T)Marshal.PtrToStructure(ptr + size * i, typeof(T));
                }

                return result;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }

        // Enum values
        public static string Prefix(this AbilityType value)
        {
            switch (value)
            {
                case AbilityType.Misc:
                case AbilityType.JobTrait:
                    return "/echo";
                case AbilityType.JobAbility:
                case AbilityType.CorsairRoll:
                case AbilityType.CorsairShot:
                case AbilityType.Samba:
                case AbilityType.Waltz:
                case AbilityType.Step:
                case AbilityType.Jig:
                case AbilityType.Flourish1:
                case AbilityType.Flourish2:
                case AbilityType.Flourish3:
                case AbilityType.Scholar:
                case AbilityType.Rune:
                case AbilityType.Ward:
                case AbilityType.Effusion:
                    return "/jobability";
                case AbilityType.WeaponSkill:
                    return "/weaponskill";
                case AbilityType.MonsterSkill:
                    return "/monsterskill";
                case AbilityType.PetCommand:
                case AbilityType.BloodPactWard:
                case AbilityType.BloodPactRage:
                case AbilityType.Monster:
                    return "/pet";
            }

            return "/unknown";
        }

        public static string Prefix(this MagicType value)
        {
            switch (value)
            {
                case MagicType.WhiteMagic:
                case MagicType.BlackMagic:
                case MagicType.SummonerPact:
                case MagicType.BlueMagic:
                case MagicType.Geomancy:
                case MagicType.Trust:
                    return "/magic";

                case MagicType.BardSong:
                    return "/song";

                case MagicType.Ninjutsu:
                    return "/ninjutsu";
            }

            return "/unknown";
        }

        public static object Parse(this string value)
        {
            var str = value;
            if (!str.StartsWith("{"))
            {
                return
                    int.TryParse(str, out var resint) ? resint :
                    float.TryParse(str, out var resfloat) ? resfloat :
                    bool.TryParse(str, out var resbool) ? (object)resbool :
                    str;
            }

            str = str.Substring(1, str.Length - 2);
            if (!str.StartsWith("["))
            {
                return str.Split(',').Select(Parse).ToList();
            }

            return str.Split(',').Select(kvp => kvp.Split('=')).ToDictionary(kvp => Parse(kvp[0].Substring(1, kvp[0].Length - 2)), kvp => Parse(kvp[1]));
        }

        private static int CountBits(byte b)
        {
            int count = 0;

            while (b != 0)
            {
                if ((b & 1) != 0)
                {
                    count++;
                }

                b >>= 1;
            }

            return count;
        }
    }
}
