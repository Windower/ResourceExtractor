// <copyright file="ImageHeader.cs" company="Windower Team">
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
    using System.Runtime.InteropServices;

    enum ImageType
    {
        Unknown,
        Bitmap,
        DXT1,
        DXT2,
        DXT3,
        DXT4,
        DXT5,
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
    struct ImageHeader
    {
        private uint structLength;
        private int width;
        private int height;
        private ushort planes;
        private ushort bitCount;
        private uint compression;
        private uint imageSize;
        private uint horizontalResolution;
        private uint verticalResolution;
        private uint usedColors;
        private uint importantColors;
        private uint type;

        public uint StructLength
        {
            get
            {
                return structLength;
            }
        }

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
        }

        public ushort Planes
        {
            get
            {
                return planes;
            }
        }

        public ushort BitCount
        {
            get
            {
                return bitCount;
            }
        }

        public uint Compression
        {
            get
            {
                return compression;
            }
        }

        public uint ImageSize
        {
            get
            {
                return imageSize;
            }
        }

        public uint HorizontalResolution
        {
            get
            {
                return horizontalResolution;
            }
        }

        public uint VerticalResolution
        {
            get
            {
                return verticalResolution;
            }
        }

        public uint UsedColors
        {
            get
            {
                return usedColors;
            }
        }

        public uint ImportantColors
        {
            get
            {
                return importantColors;
            }
        }

        public ImageType Type
        {
            get
            {
                switch (type - 0x44585430)
                {
                case 1:
                    return ImageType.DXT1;
                case 2:
                    return ImageType.DXT2;
                case 3:
                    return ImageType.DXT3;
                case 4:
                    return ImageType.DXT4;
                case 5:
                    return ImageType.DXT5;
                }

                if (type == 0x0000000A)
                {
                    return ImageType.Bitmap;
                }

                return ImageType.Unknown;
            }
        }
    }
}
