// <copyright file="BitmapParser.cs" company="Windower Team">
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
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;

    internal static class BitmapParser
    {
        internal static Bitmap Parse(BinaryReader reader, ImageHeader header, bool ignoreAlpha = false)
        {
            var Result = new Bitmap(header.Width, header.Height, PixelFormat.Format32bppArgb);
            var Raw = Result.LockBits(new Rectangle(0, 0, Result.Width, Result.Height), ImageLockMode.WriteOnly, Result.PixelFormat);

            var PixelCount = header.Height * header.Width;

            unsafe
            {
                var Buffer = (byte*)Raw.Scan0;

                Color[] Palette = null;
                byte[] BitFields = null;
                bool EightCount = header.BitCount == 8;
                if (EightCount)
                { // 8-bit, with palette
                    Palette = new Color[256];
                    for (var i = 0; i < 256; ++i)
                    {
                        Palette[i] = ReadColor(reader, 32);
                    }

                    BitFields = new byte[PixelCount];
                    var PixelStride = PixelCount / Result.Width;
                    BitFields = reader.ReadBytes(PixelCount);
                }

                for (var Pixel = 0; Pixel < PixelCount; ++Pixel)
                {
                    var Color = EightCount ? Palette[BitFields[Pixel]] : ReadColor(reader, header.BitCount);
                    Buffer[4 * Pixel + 0] = Color.B;
                    Buffer[4 * Pixel + 1] = Color.G;
                    Buffer[4 * Pixel + 2] = Color.R;
                    Buffer[4 * Pixel + 3] = ignoreAlpha ? (byte)255 : Color.A;
                }
            }

            Result.UnlockBits(Raw);

            Result.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return Result;
        }

        private static Color DecodeRGB565(ushort C)
        {
            return Color.FromArgb((C & 0xF800) >> 8, (C & 0x07E0) >> 3, (C & 0x001F) << 3);
        }

        public static Color ReadColor(BinaryReader reader, int bitDepth)
        {
            switch (bitDepth)
            {
            case 8:
                {
                    int GrayScale = reader.ReadByte();
                    return Color.FromArgb(GrayScale, GrayScale, GrayScale);
                }
            case 16: return DecodeRGB565(reader.ReadUInt16());
            case 24:
            case 32:
                {
                    int B = reader.ReadByte();
                    int G = reader.ReadByte();
                    int R = reader.ReadByte();
                    int A = 255;
                    if (bitDepth == 32)
                    {
                        byte SemiAlpha = reader.ReadByte();
                        if (SemiAlpha < 0x80)
                        {
                            A = 2 * SemiAlpha;
                        }
                    }
                    return Color.FromArgb(A, R, G, B);
                }
            default:
                return Color.HotPink;
            }
        }
    }
}
