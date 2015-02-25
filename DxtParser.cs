// <copyright file="DxtParser.cs" company="Windower Team">
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
    using System.Globalization;
    using System.IO;

    internal static class DxtParser
    {
        internal static Bitmap Parse(BinaryReader reader, ImageHeader header, bool ignoreAlpha = false)
        {
            var Format = (header.Type == ImageType.DXT2 || header.Type == ImageType.DXT4) ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb;
            var Result = new Bitmap(header.Width, header.Height, Format);
            try
            {
                var Raw = Result.LockBits(new Rectangle(0, 0, Result.Width, Result.Height), ImageLockMode.WriteOnly, Result.PixelFormat);

                var TexelBlockCount = (header.Width * header.Height) / (4 * 4);
                unsafe
                {
                    var Buffer = (byte*)Raw.Scan0;

                    for (var Texel = 0; Texel < TexelBlockCount; ++Texel)
                    {
                        var TexelBlock = ReadTexelBlock(reader, header.Type);
                        var PixelOffsetX = 4 * (Texel % (header.Width / 4));
                        var PixelOffsetY = 4 * (Texel / (header.Width / 4)) * (Raw.Stride / 4);

                        var Index = 4 * (PixelOffsetX + PixelOffsetY);
                        for (var Y = 0; Y < 4; ++Y, Index += Raw.Stride - 16)
                        {
                            for (var X = 0; X < 4; ++X, Index += 4)
                            {
                                var Lookup = X + 4 * Y;
                                Buffer[Index + 0] = TexelBlock[Lookup].B;
                                Buffer[Index + 1] = TexelBlock[Lookup].G;
                                Buffer[Index + 2] = TexelBlock[Lookup].R;
                                Buffer[Index + 3] = ignoreAlpha ? (byte)255 : TexelBlock[Lookup].A;
                            }
                        }
                    }
                }

                Result.UnlockBits(Raw);
            }
            catch
            {
                Result.Dispose();
                throw;
            }

            return Result;
        }

        private static Color[] ReadTexelBlock(BinaryReader reader, ImageType type)
        {
            ulong AlphaBlock = 0;

            if (type == ImageType.DXT2 || type == ImageType.DXT3 || type == ImageType.DXT4 || type == ImageType.DXT5)
            {
                AlphaBlock = reader.ReadUInt64();
            }
            else if (type != ImageType.DXT1)
            {
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Format {0} not recognized.", type));
            }

            ushort C0 = reader.ReadUInt16();
            ushort C1 = reader.ReadUInt16();
            Color[] Colors = new Color[4];
            Colors[0] = DecodeRGB565(C0);
            Colors[1] = DecodeRGB565(C1);
            if (C0 > C1 || type != ImageType.DXT1)
            { // opaque, 4-color
                Colors[2] = Color.FromArgb((2 * Colors[0].R + Colors[1].R + 1) / 3, (2 * Colors[0].G + Colors[1].G + 1) / 3, (2 * Colors[0].B + Colors[1].B + 1) / 3);
                Colors[3] = Color.FromArgb((2 * Colors[1].R + Colors[0].R + 1) / 3, (2 * Colors[1].G + Colors[0].G + 1) / 3, (2 * Colors[1].B + Colors[0].B + 1) / 3);
            }
            else
            { // 1-bit alpha, 3-color
                Colors[2] = Color.FromArgb((Colors[0].R + Colors[1].R) / 2, (Colors[0].G + Colors[1].G) / 2, (Colors[0].B + Colors[1].B) / 2);
                Colors[3] = Color.Transparent;
            }
            uint CompressedColor = reader.ReadUInt32();
            Color[] DecodedColors = new Color[16];
            for (int i = 0; i < 16; ++i)
            {
                if (type == ImageType.DXT2 || type == ImageType.DXT3 || type == ImageType.DXT4 || type == ImageType.DXT5)
                {
                    int A = 255;
                    if (type == ImageType.DXT2 || type == ImageType.DXT3)
                    {
                        // Seems to be 8 maximum; so treat 8 as 255 and all other values as 3-bit alpha
                        A = (int)((AlphaBlock >> (4 * i)) & 0xF);
                        if (A >= 8)
                        {
                            A = 0xFF;
                        }
                        else
                        {
                            A <<= 5;
                        }
                    }
                    else
                    { // Interpolated alpha
                        int[] Alphas = new int[8];
                        Alphas[0] = (byte)((AlphaBlock >> 0) & 0xFF);
                        Alphas[1] = (byte)((AlphaBlock >> 8) & 0xFF);
                        if (Alphas[0] > Alphas[1])
                        {
                            Alphas[2] = (Alphas[0] * 6 + Alphas[1] * 1 + 3) / 7;
                            Alphas[3] = (Alphas[0] * 5 + Alphas[1] * 2 + 3) / 7;
                            Alphas[4] = (Alphas[0] * 4 + Alphas[1] * 3 + 3) / 7;
                            Alphas[5] = (Alphas[0] * 3 + Alphas[1] * 4 + 3) / 7;
                            Alphas[6] = (Alphas[0] * 2 + Alphas[1] * 5 + 3) / 7;
                            Alphas[7] = (Alphas[0] * 1 + Alphas[1] * 6 + 3) / 7;
                        }
                        else
                        {
                            Alphas[2] = (Alphas[0] * 4 + Alphas[1] * 1 + 2) / 5;
                            Alphas[3] = (Alphas[0] * 3 + Alphas[1] * 2 + 2) / 5;
                            Alphas[4] = (Alphas[0] * 2 + Alphas[1] * 3 + 2) / 5;
                            Alphas[5] = (Alphas[0] * 1 + Alphas[1] * 4 + 2) / 5;
                            Alphas[6] = 0;
                            Alphas[7] = 255;
                        }
                        ulong AlphaMatrix = (AlphaBlock >> 16) & 0xFFFFFFFFFFFFL;
                        A = Alphas[(AlphaMatrix >> (3 * i)) & 0x7];
                    }
                    DecodedColors[i] = Color.FromArgb(A, Colors[CompressedColor & 0x3]);
                }
                else
                {
                    DecodedColors[i] = Colors[CompressedColor & 0x3];
                }
                CompressedColor >>= 2;
            }
            return DecodedColors;
        }

        private static Color DecodeRGB565(ushort C)
        {
            return Color.FromArgb((C & 0xF800) >> 8, (C & 0x07E0) >> 3, (C & 0x001F) << 3);
        }
    }
}
