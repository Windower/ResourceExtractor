using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ResourceExtractor; 

internal static class ExtensionMethods {
	public static R Let<T, R>(this T value, Func<T, R> fn) =>
		fn(value);

	public static void RotateRight(this byte[] data, int count) {
		for (var i = 0; i < data.Length; i++) {
			var b = data[i];
			data[i] = (byte) (b >> count | b << (8 - count));
		}
	}

	public static void Decode(this byte[] data) {
		if (data.Length < 13) {
			return;
		}

		var key = (Math.Abs(CountBits(data[2]) - CountBits(data[11]) + CountBits(data[12])) % 5) switch {
			0 => 7,
			1 => 1,
			2 => 6,
			3 => 2,
			4 => 5,
			_ => 0,
		};

		data.RotateRight(key);
	}

	public static T Read<T>(this Stream stream) {
		var size = Marshal.SizeOf(typeof(T));
		var data = new byte[size];
		stream.Read(data, 0, data.Length);

		var ptr = IntPtr.Zero;
		try {
			ptr = Marshal.AllocHGlobal(data.Length);
			Marshal.Copy(data, 0, ptr, data.Length);
			return (T) Marshal.PtrToStructure(ptr, typeof(T));
		} finally {
			if (ptr != IntPtr.Zero) {
				Marshal.FreeHGlobal(ptr);
			}
		}
	}

	public static T Read<T>(this Stream stream, uint offset) {
		stream.Position = offset;
		return Read<T>(stream);
	}

	public static T[] ReadArray<T>(this Stream stream, int count, uint offset) {
		stream.Position = offset;
		return stream.ReadArray<T>(count);
	}

	public static T[] ReadArray<T>(this Stream stream, int count) {
		var size = Marshal.SizeOf(typeof(T));
		var data = new byte[size * count];
		stream.Read(data, 0, data.Length);

		var ptr = IntPtr.Zero;
		try {
			ptr = Marshal.AllocHGlobal(data.Length);
			Marshal.Copy(data, 0, ptr, data.Length);

			var result = new T[count];

			for (var i = 0; i < count; i++) {
				result[i] = (T) Marshal.PtrToStructure(ptr + size * i, typeof(T));
			}

			return result;
		} finally {
			if (ptr != IntPtr.Zero) {
				Marshal.FreeHGlobal(ptr);
			}
		}
	}

	public static string Prefix(this AbilityType value) {
		return value switch {
			AbilityType.Misc or AbilityType.JobTrait => "/echo",
			AbilityType.JobAbility or AbilityType.CorsairRoll or AbilityType.CorsairShot or AbilityType.Samba or AbilityType.Waltz or AbilityType.Step or AbilityType.Jig or AbilityType.Flourish1 or AbilityType.Flourish2 or AbilityType.Flourish3 or AbilityType.Scholar or AbilityType.Rune or AbilityType.Ward or AbilityType.Effusion => "/jobability",
			AbilityType.WeaponSkill => "/weaponskill",
			AbilityType.MonsterSkill => "/monsterskill",
			AbilityType.PetCommand or AbilityType.BloodPactWard or AbilityType.BloodPactRage or AbilityType.Monster => "/pet",
			_ => "/unknown",
		};
	}

	public static string Prefix(this MagicType value) {
		return value switch {
			MagicType.WhiteMagic or MagicType.BlackMagic or MagicType.SummonerPact or MagicType.BlueMagic or MagicType.Geomancy or MagicType.Trust => "/magic",
			MagicType.BardSong => "/song",
			MagicType.Ninjutsu => "/ninjutsu",
			_ => "/unknown",
		};
	}

	public static object Parse(this string value) {
		var str = value;
		if (!str.StartsWith('{')) {
			return
				Int32.TryParse(str, out var resint) ? resint :
				Single.TryParse(str, out var resfloat) ? resfloat :
				Boolean.TryParse(str, out var resbool) ? resbool :
				str;
		}

		str = str[1..^1];
		if (!str.StartsWith('[')) {
			return str.Split(',').Select(Parse).ToList();
		}

		return str.Split(',')
			.Select(kvp => kvp.Split('='))
			.ToDictionary(kvp => Parse(kvp[0][1..^1]), kvp => Parse(kvp[1]));
	}

	private static int CountBits(byte b) {
		var count = 0;

		while (b != 0) {
			if ((b & 1) != 0) {
				count++;
			}

			b >>= 1;
		}

		return count;
	}
}
