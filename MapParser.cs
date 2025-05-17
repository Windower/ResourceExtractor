using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

namespace ResourceExtractor; 

[SupportedOSPlatform("windows")]
public static class MapParser {
	public static void Extract(string outDir) {
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ResourceExtractor.MapDats.json");
		var dats = JsonSerializer.Deserialize<IDictionary<string, IDictionary<string, ushort>>>(stream);

		foreach (var (zone, maps) in dats) {
			foreach (var (map, datId) in maps) {
				using var dat = File.Open(Program.GetPath(datId), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var image = ImageParser.Parse(dat, true);
				using var file = File.Open($"{outDir}/resources/maps/{zone}_{map}.png", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
				image.Save(file, ImageFormat.Png);
			}
		}
	}

	#region JSON Comments/Posterity
	//There are a number of duplicate or unused map DAT's.
	//These are values that have been removed from MapDats.json.
	//"2": {"0": 5689},                     //0 is not used in game.
	//"29": {"2": 5729},                    //Duplicate of map 1.
	//"30": {"2": 5731},                    //Duplicate of map 1.
	//"44": {"2": 5748},                    //Duplicate of map 1.
	//"140": {"15": 5341},                  //15 is not used in game.
	//"142": {"0": 5343},                   //Duplicate of map 1.
	//"169": {"3": 5633},                   //Duplicate of map 2.
	//"171": {"0": 5408,                    //0 is a dummy map.
	//"173": {"0": 5410},                   //0 is not used in game.
	//"174": {"0": 5411},                   //0 is not used in game.
	//"190": {"1001": 5430},                //Duplicate of map 1.
	//"191": {"0": 5431},                   //Duplicate of map 1.
	//"205": {"1015": 5456, "1016": 5457    //These are maps that never made it to game, different zone name.
	//"205": {"18": 5685                    //18 is not used in game.
	//"226": {"0": 5475},                   //0 is a dummy map.
	//"242": {"0": 5490},                   //0 is a dummy map.
	#endregion
}
