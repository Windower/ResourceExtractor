// <copyright file="MapDats.cs" company="Windower Team">
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
    using System.Collections.Generic;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Web.Script.Serialization;

    public static class MapParser
    {
        public static void Extract()
        {
            Console.Write("Extract map data? (y/n) ");
            Console.CursorVisible = true;
            var Key = Console.ReadKey();
            Console.CursorVisible = false;
            Console.Write("\n");

            if (Key.KeyChar != 'y')
            {
                return;
            }

            Program.DisplayMessage("Extracting map data...");

            var Serializer = new JavaScriptSerializer();
            var DatLut = Serializer.Deserialize<IDictionary<string, IDictionary<string, ushort>>>(File.ReadAllText("MapDats.json"));

            bool Success = false;
            try
            {
                foreach (var ZonePair in DatLut)
                {
                    var Zone = ZonePair.Key;
                    foreach (var MapPair in ZonePair.Value)
                    {
                        var Map = MapPair.Key;

                        using (FileStream Stream = File.Open(Program.GetPath(MapPair.Value), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var Image = ImageParser.Parse(Stream);
                            using (FileStream OutFile = File.Open(string.Format("resources/maps/{0}_{1}.png", Zone, Map), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                            {
                                Image.Save(OutFile, ImageFormat.Png);
                            }
                        }
                    }
                }

                Success = true;
            }
            finally
            {
                Program.DisplayResult(Success);
            }
        }
    }
}
