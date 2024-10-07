using System.Collections.Generic;
using System.IO;

namespace ResourceExtractor; 

internal class ATParser {
	internal static dynamic[] Parse(Stream stream, string key) {
		var objects = new List<ModelObject>();

		using (var reader = new BinaryReader(stream)) {
			while (stream.Position < stream.Length) {
				reader.ReadByte();
				var language = reader.ReadByte();
				var id = 0x100 * reader.ReadByte() | reader.ReadByte();

				dynamic entry = new ModelObject();
				entry.id = id;

				string name;

				if ((id & 0xFF) == 0) {
					reader.ReadBytes(0x20);
					var nameData = reader.ReadBytes(0x20);
					reader.ReadInt32();
					reader.ReadInt32();

					var length = 0;
					while (nameData[length] != 0x00) {
						++length;
					}

					name = ShiftJISFF11Encoding.ShiftJISFF11.GetString(nameData, 0, length);
				} else {
					var length = reader.ReadByte();
					name = ShiftJISFF11Encoding.ShiftJISFF11.GetString(reader.ReadBytes(length), 0, length - 1);

					if (language == 1) {
						reader.ReadBytes(reader.ReadByte());
					}
				}

				entry[key] = name;

				objects.Add(entry);
			}
		}

		return objects.ToArray();
	}
}
