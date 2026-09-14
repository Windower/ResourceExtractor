using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ResourceExtractor; 

internal class DialogParser {
	internal static ModelObject[] Parse(Stream stream, string key) {
		var header = stream.Read<Header>();

		if (header.FileSize != stream.Length) {
			throw new InvalidOperationException("Data is corrupt.");
		}

		var data = new byte[header.ContentSize];
		stream.Read(data, 0, data.Length);
		for (var i = 0; i < data.Length; ++i) {
			data[i] ^= 0x80;
		}

		using var dataStream = new MemoryStream(data);
		var tableSize = dataStream.Peek<int>() / 4;
		var table = dataStream.ReadArray<int>(tableSize);

		var objects = new ModelObject[tableSize];

		for (var i = 0; i < table.Length; ++i) {
			var offset = table[i];
			var length = (i + 1 < table.Length ? table[i + 1] : data.Length) - offset;

			while (data[offset + length - 1] == 0) {
				--length;
			}

			objects[i] = new() {
				{key, ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, offset, length)}
			};
		}

		return objects;
	}

	[StructLayout(LayoutKind.Sequential)]
	private readonly struct Header {
		private readonly int contentSize;

		public int ContentSize => contentSize - 0x10000000;
		public int FileSize => ContentSize + 4;
	}
}
