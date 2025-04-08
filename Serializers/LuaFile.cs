using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ResourceExtractor.Serializers; 

internal class LuaFile(string name) {
	public void Add(dynamic e) {
		var el = new LuaElement(e);
		Elements.Add(el);
		Keys.UnionWith(el.Keys);
	}

	public void Save() {
		using var file = new StreamWriter(Path.Combine(Directory.GetCurrentDirectory(), "resources", "lua", FormattableString.Invariant($"{name}.lua"))) {
			NewLine = "\n",
		};
		var words = name.Split('_').Select(str => Char.ToUpperInvariant(str[0]) + str[1..]);
		file.WriteLine("-- Automatically generated file: {0}", String.Join(" ", words));
		file.WriteLine();
		file.WriteLine("return {");

		foreach (var e in Elements.OrderBy(e => e.ID)) {
			file.WriteLine("    [{0}] = {1},", e.ID, e);
		}

		file.WriteLine("}}, {0}", "{" + String.Join(", ", Keys.Select(k => "\"" + k + "\"")) + "}");
		file.WriteLine();
		file.WriteLine("--[[");
		file.WriteLine("Copyright © 2013-{0}, Windower", DateTime.Now.Year);
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

	private List<LuaElement> Elements { get; } = [];
	private HashSet<string> Keys { get; } = [];
}
