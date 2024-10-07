using System.Collections.Generic;
using System.IO;

namespace ResourceExtractor; 

internal class DatParser {
	private DatParser() { }

	public static dynamic[] Parse(Stream stream, IDictionary<int, string> fields) {
		var format = stream.Read<ulong>();
		stream.Position = 0;

		// DMsg format
		if (format == 0x00000067736D5F64) {
			return DMsgParser.Parse(stream, fields);
		}

		// Dialog format
		if ((format & 0x10000000) > 0 && format % 0x10000000 == (ulong) stream.Length - 4) {
			return DialogParser.Parse(stream, fields[0]);
		}

		// Auto-translate files (no specific format)
		if ((format & 0xFFFFFCFF) == 0x00010002) {
			return ATParser.Parse(stream, fields[0]);
		}

		throw new InvalidDataException("Unknown DAT format.");
	}
}
