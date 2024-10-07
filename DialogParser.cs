using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ResourceExtractor; 

internal class DialogParser {
	internal static dynamic[] Parse(Stream stream, string key) {
		var header = stream.Read<Header>(0);
		// First valid value was included in the header to get the table size
		stream.Position -= 4;

		if (header.FileSize != stream.Length - 4) {
			throw new InvalidOperationException("Data is corrupt.");
		}

		var data = new byte[header.FileSize];
		stream.Read(data, 0, data.Length);
		for (var i = 0; i < data.Length; ++i) {
			data[i] ^= 0x80;
		}
		int[] table;
		using (var datastream = new MemoryStream(data)) {
			table = datastream.ReadArray<int>(header.TableSize);
		}

		dynamic objects = new ModelObject[header.TableSize];

		for (var i = 0; i < table.Length; ++i) {
			var offset = table[i];
			var length = (i + 1 < table.Length ? table[i + 1] : data.Length) - offset;

			while (data[offset + length - 1] == 0) {
				--length;
			}

			objects[i] = new ModelObject {
				{key, ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, offset, length)}
			};
		}

		return objects;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
	private readonly struct Header {
		private readonly int fileSize;
		private readonly int tableSize;

		public int FileSize => fileSize - 0x10000000;
		public int TableSize => (int) (tableSize ^ 0x80808080) / 4;
	}
}
