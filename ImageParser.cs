// <copyright file="ImageParser.cs" company="Windower Team">
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
    using System.Drawing;
    using System.IO;

    internal class ImageParser
    {
        internal static Bitmap Parse(Stream stream, bool ignoreAlpha = false)
        {
            Bitmap Result = null;

            var Flag = stream.Read<byte>(0x30);
            var Header = stream.Read<ImageHeader>(0x41);

            using (BinaryReader Reader = new BinaryReader(stream))
            {
                switch (Header.Type)
                {
                case ImageType.DXT1:
                case ImageType.DXT2:
                case ImageType.DXT3:
                case ImageType.DXT4:
                case ImageType.DXT5:
                    // Unknown 8 bytes
                    Reader.ReadBytes(8);

                    Result = DxtParser.Parse(Reader, Header, ignoreAlpha);
                    break;
                case ImageType.Bitmap:
                    Result = BitmapParser.Parse(Reader, Header, ignoreAlpha);
                    break;
                default:
                    throw new InvalidDataException(string.Format("Image type {0} not supported: {1}", Header.Type, Flag));
                }
            }

            return Result;
        }
    }
}
