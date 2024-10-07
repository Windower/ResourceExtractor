using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace ResourceExtractor; 

internal class DMsgParser {
	internal static dynamic[] Parse(Stream stream, IDictionary<int, string> fields) {
		var header = stream.Read<Header>(0);

		if (header.Format != 0x00000067736D5F64) {
			throw new InvalidOperationException("Invalid data format.");
		}

		if (header.Version != 0x0000000300000003) {
			throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid format version. [{0:X8}]", header.Version));
		}

		long[] table;
		if (header.TableSize != 0) {
			if (header.TableSize != header.Count * 8 || header.EntrySize != 0) {
				throw new InvalidOperationException("Data is corrupt.");
			}

			table = stream.ReadArray<long>(header.Count, header.HeaderSize);
		} else {
			if (header.DataSize != header.Count * header.EntrySize) {
				throw new InvalidOperationException("Data is corrupt.");
			}

			table = [];
		}

		stream.Position = header.HeaderSize + header.TableSize;
		var data = new byte[header.DataSize];
		stream.Read(data, 0, data.Length);

		if (header.Encrypted) {
			for (var i = 0; i < table.Length; i++) {
				table[i] = ~table[i];
			}

			for (var i = 0; i < data.Length; i++) {
				data[i] = (byte) ~data[i];
			}
		}

		dynamic objects = new ModelObject[header.Count];

		var length = (int) header.EntrySize;
		for (var i = 0; i < objects.Length; i++) {
			int offset;
			if (header.TableSize != 0) {
				length = (int) (table[i] >> 32);
				offset = (int) table[i];
			} else {
				offset = i * (int) header.EntrySize;
			}

			var count = BitConverter.ToInt32(data, offset);

			dynamic resource = new ModelObject();

			for (var j = 0; j < count; j++) {
				if (!fields.ContainsKey(j)) {
					continue;
				}

				var entryoffset = BitConverter.ToInt32(data, offset + j * 8 + 4) + offset;
				var entrytype = BitConverter.ToInt32(data, offset + j * 8 + 8);

				dynamic value;
				switch (entrytype) {
					case 0:
						entryoffset += 0x1C;

						var stringlength = 0;
						for (var k = 0; k < length; k++) {
							if (data[entryoffset + k] == 0) {
								break;
							}

							stringlength++;
						}

						value = ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, entryoffset, stringlength);
						break;
					case 1:
						value = BitConverter.ToInt32(data, entryoffset);
						break;
					default:
						value = null;
						break;
				}

				resource[fields[j]] = value;
			}

			objects[i] = resource;
		}

		return objects;
	}

	[SuppressMessage("Style", "IDE0032:Use auto property", Justification = "Messes up struct layout")]
	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 64)]
	private readonly struct Header {
		private readonly long format;
		private readonly short unknown;
		private readonly byte encrypted;
		private readonly long version;
		private readonly uint filesize;
		private readonly uint headersize;
		private readonly uint tablesize;
		private readonly uint entrysize;
		private readonly int datasize;
		private readonly int count;

		public long Format => format;
		public bool Encrypted => encrypted != 0;
		public long Version => version;
		public uint HeaderSize => headersize;
		public uint TableSize => tablesize;
		public uint EntrySize => entrysize;
		public int DataSize => datasize;
		public int Count => count;
	}
}
