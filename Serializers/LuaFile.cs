using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace ResourceExtractor.Serializers; 

internal static class LuaFile {
	public static void Write(string outDir, string name, IImmutableList<dynamic> entries) {
		using var file = new StreamWriter(Path.Combine(outDir, FormattableString.Invariant($"{name}.lua"))) {
			NewLine = "\n",
		};
		var words = name.Split('_').Select(str => Char.ToUpperInvariant(str[0]) + str[1..]);
		file.WriteLine("-- Automatically generated file: {0}", String.Join(" ", words));
		file.WriteLine();
		file.WriteLine("return {");

		var keys = new HashSet<string>();
		foreach (var entry in entries.Select(entry => new LuaEntry(entry)).OrderBy(entry => entry.ID)) {
			file.WriteLine($"    [{entry.ID}] = {entry},");
			keys.UnionWith(entry.Keys);
		}

		file.WriteLine($"}}, {{{String.Join(", ", keys.Select(key => $"\"{key}\""))}}}");
		file.WriteLine();
		file.WriteLine("--[[");
		file.WriteLine($"Copyright © 2013-{DateTime.Now.Year}, Windower");
		file.WriteLine("All rights reserved.");
		file.WriteLine();
		file.WriteLine("Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:");
		file.WriteLine();
		file.WriteLine("    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.");
		file.WriteLine("    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.");
		file.WriteLine("    * Neither the name of Windower nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.");
		file.WriteLine();
		file.WriteLine("THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Windower BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.");
		file.WriteLine("]]");
	}
}
