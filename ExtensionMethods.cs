// <copyright file="ExtensionMethods.cs" company="Windower Team">
// Copyright © 2013 Windower Team
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
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    internal static class ExtensionMethods
    {
        public static IList<T> ToList<T>(this T value) where T : struct, IConvertible
        {
            if (!typeof(T).IsDefined(typeof(FlagsAttribute), false))
            {
                throw new InvalidOperationException("T must be an enumeration type with the [Flags] attribute.");
            }

            List<T> results = new List<T>();

            var flags = Enum.GetValues(typeof(T));
            Array.Reverse(flags);

            long temp = value.ToInt64(null);
            foreach (T flag in flags)
            {
                long f = flag.ToInt64(null);
                if (f != 0 && (temp & f) == f)
                {
                    temp &= ~f;
                    results.Insert(0, flag);
                }
            }

            return results;
        }

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

            int count = CountBits(data[2]) - CountBits(data[11]) + CountBits(data[12]);
            count = count < 0 ? -count : count;
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

        private static T[] ReadArray<T>(this Stream stream, int count)
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

        public static T[] ReadArray<T>(this Stream stream, int count, uint offset)
        {
            stream.Position = offset;
            return ReadArray<T>(stream, count);
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
