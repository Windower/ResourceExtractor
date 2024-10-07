using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace ResourceExtractor; 

internal class DMsgStringList : IList<IList<object>> {
	private readonly IList<object>[] objects;

	internal DMsgStringList(Stream stream) {
		var header = stream.Read<Header>(0);

		if (header.Format != 0x00000067736D5F64) {
			throw new InvalidOperationException("Invalid data format.");
		}

		if (header.Version != 0x0000000300000003) {
			throw new InvalidOperationException(FormattableString.Invariant($"Invalid format version. [{header.Version:X8}]"));
		}

		if (header.TableSize != 0) {
			if (header.TableSize != header.Count * 8 || header.EntrySize != 0) {
				throw new InvalidOperationException("Data is corrupt.");
			}

			var table = stream.ReadArray<long>(header.Count, header.HeaderSize);
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

			objects = new IList<object>[header.Count];

			for (var i = 0; i < objects.Length; i++) {
				var offset = (int) table[i];
				var length = (int) (table[i] >> 32);

				var count = BitConverter.ToInt32(data, offset);

				var s = new object[count];

				for (var j = 0; j < count; j++) {
					var entryoffset = BitConverter.ToInt32(data, offset + j * 8 + 4) + offset;
					var entrytype = BitConverter.ToInt32(data, offset + j * 8 + 8);

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

							s[j] = ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, entryoffset, stringlength);
							break;
						case 1:
							s[j] = BitConverter.ToInt32(data, entryoffset);
							break;
					}
				}

				objects[i] = s;
			}
		} else {
			if (header.DataSize != header.Count * header.EntrySize) {
				throw new InvalidOperationException("Data is corrupt.");
			}

			stream.Position = header.HeaderSize + header.TableSize;
			var data = new byte[header.DataSize];
			stream.Read(data, 0, data.Length);

			if (header.Encrypted) {
				for (var i = 0; i < data.Length; i++) {
					data[i] = (byte) ~data[i];
				}
			}

			objects = new IList<object>[header.Count];

			for (var i = 0; i < objects.Length; i++) {
				var offset = i * (int) header.EntrySize;

				var count = BitConverter.ToInt32(data, offset);

				var s = new object[count];

				for (var j = 0; j < count; j++) {
					var entryoffset = BitConverter.ToInt32(data, offset + j * 8 + 4) + offset;
					var entrytype = BitConverter.ToInt32(data, offset + j * 8 + 8);

					switch (entrytype) {
						case 0:
							entryoffset += 0x1C;

							var length = 0;
							for (var k = 0; k < header.EntrySize; k++) {
								if (data[entryoffset + k] == 0) {
									break;
								}

								length++;
							}

							s[j] = ShiftJISFF11Encoding.ShiftJISFF11.GetString(data, entryoffset, length);
							break;
						case 1:
							s[j] = BitConverter.ToInt32(data, entryoffset);
							break;
					}
				}

				objects[i] = s;
			}
		}
	}

	bool ICollection<IList<object>>.IsReadOnly => true;

	public int Count => objects.Length;

	public IList<object> this[int index] {
		get => objects[index];

		set => throw new NotSupportedException();
	}

	int IList<IList<object>>.IndexOf(IList<object> item) {
		return ((IList<IList<object>>) objects).IndexOf(item);
	}

	bool ICollection<IList<object>>.Contains(IList<object> item) {
		return ((ICollection<IList<object>>) objects).Contains(item);
	}

	void ICollection<IList<object>>.CopyTo(IList<object>[] array, int arrayIndex) {
		objects.CopyTo(array, arrayIndex);
	}

	public IEnumerator<IList<object>> GetEnumerator() {
		return ((IList<IList<object>>) objects).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return objects.GetEnumerator();
	}

	public void Add(IList<object> item) {
		throw new NotSupportedException();
	}

	public void Insert(int index, IList<object> item) {
		throw new NotSupportedException();
	}

	public bool Remove(IList<object> item) {
		throw new NotSupportedException();
	}

	public void RemoveAt(int index) {
		throw new NotSupportedException();
	}

	public void Clear() {
		throw new NotSupportedException();
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
