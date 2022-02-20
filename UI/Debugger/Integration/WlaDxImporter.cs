using Mesen.GUI.Config;
using Mesen.GUI.Debugger.Labels;
using Mesen.GUI.Debugger.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mesen.GUI.Debugger.Integration
{
	public class WlaDxImporter : ISymbolProvider
	{
		private Dictionary<int, SourceFileInfo> _sourceFiles = new Dictionary<int, SourceFileInfo>();
		private Dictionary<string, AddressInfo> _addressByLine = new Dictionary<string, AddressInfo>();
		private Dictionary<string, SourceCodeLocation> _linesByAddress = new Dictionary<string, SourceCodeLocation>();
		private Dictionary<string, CodeLabel> _labelDefinitions = new Dictionary<string, CodeLabel>();
		private List<SourceSymbol> _sourceSymbols = new List<SourceSymbol>();
		private Dictionary<string, int> _labelNamesToSourceSymbols = new Dictionary<string, int>();

		public DateTime SymbolFileStamp { get; private set; }
		public string SymbolPath { get; private set; }

		public List<SourceFileInfo> SourceFiles { get { return _sourceFiles.Values.ToList(); } }

		public AddressInfo? GetLineAddress(SourceFileInfo file, int lineIndex)
		{
			AddressInfo address;
			if(_addressByLine.TryGetValue(file.Name.ToString() + "_" + lineIndex.ToString(), out address)) {
				return address;
			}
			return null;
		}

		public SourceCodeLocation GetSourceCodeLineInfo(AddressInfo address)
		{
			string key = address.Type.ToString() + address.Address.ToString();
			SourceCodeLocation location;
			if(_linesByAddress.TryGetValue(key, out location)) {
				return location;
			}
			return null;
		}

		public SourceSymbol GetSymbol(string word, int prgStartAddress, int prgEndAddress)
		{
			{
				CodeLabel label;

				// If we get a direct full match here, we're golden.
				if (_labelDefinitions.TryGetValue(word, out label) && _labelNamesToSourceSymbols.ContainsKey(label.Label)) {
					return _sourceSymbols[_labelNamesToSourceSymbols[label.Label]];
				}
			}

			// This fallback here is a hack, mostly to support labels with scopes/namespaces from Asar.
			// At this time, the WLA-DX format doesn't support scopes in any way, which makes this impossible to support properly.
			// If the format ever gets updated accordingly, this hack should disappear, and proper scope-based lookup should be implemented.
			foreach (KeyValuePair<string, CodeLabel> label in _labelDefinitions)
			{
				if (label.Key.EndsWith("_" + word) && _labelNamesToSourceSymbols.ContainsKey(label.Value.Label)) {
					return _sourceSymbols[_labelNamesToSourceSymbols[label.Value.Label]];
				}
			}

			return null;
		}

		public AddressInfo? GetSymbolAddressInfo(SourceSymbol symbol)
		{
			AddressInfo absAddress = (symbol.InternalSymbol as CodeLabel).GetAbsoluteAddress();
			return absAddress;
		}

		public SourceCodeLocation GetSymbolDefinition(SourceSymbol symbol)
		{
			// Currently not supported by WLA-DX format at all.
			return null;
		}

		public List<SourceSymbol> GetSymbols()
		{
			return _sourceSymbols;
		}

		public int GetSymbolSize(SourceSymbol srcSymbol)
		{
			// Currently only supported indirectly by WLA-DX format.
			// Not quite sure if worth implementing at this point in time.
			return 1;
		}

		public void Import(string path, bool silent)
		{
			string basePath = Path.GetDirectoryName(path);
			string[] lines = File.ReadAllLines(path);

			Regex labelRegex = new Regex(@"^([0-9a-fA-F]{2,4}):([0-9a-fA-F]{4}) ([^\s]*)", RegexOptions.Compiled);
			Regex fileRegex = new Regex(@"^([0-9a-fA-F]{4}) ([0-9a-fA-F]{8}) (.*)", RegexOptions.Compiled);
			Regex addrRegex = new Regex(@"^([0-9a-fA-F]{2,4}):([0-9a-fA-F]{4}) ([0-9a-fA-F]{4}):([0-9a-fA-F]{8})", RegexOptions.Compiled);

			bool isGameboy = EmuApi.GetRomInfo().CoprocessorType == CoprocessorType.Gameboy;

			for(int i = 0; i < lines.Length; i++) {
				string str = lines[i].Trim();
				if(str == "[labels]") {
					for(; i < lines.Length; i++) {
						if(lines[i].Length > 0) {
							Match m = labelRegex.Match(lines[i]);
							if(m.Success) {
								int bank = Int32.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
								string label = m.Groups[3].Value;
								label = label.Replace('.', '_').Replace(':', '_').Replace('$', '_');

								if(!LabelManager.LabelRegex.IsMatch(label)) {
									//ignore labels that don't respect the label naming restrictions
									continue;
								}

								AddressInfo absAddr;
								if(isGameboy) {
									int addr = Int32.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
									if(addr >= 0x8000) {
										AddressInfo relAddr = new AddressInfo() { Address = addr, Type = SnesMemoryType.GameboyMemory };
										absAddr = DebugApi.GetAbsoluteAddress(relAddr);
									} else {
										absAddr = new AddressInfo() { Address = bank * 0x4000 + (addr & 0x3FFF), Type = SnesMemoryType.GbPrgRom };
									}
								} else {
									int addr = (bank << 16) | Int32.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
									AddressInfo relAddr = new AddressInfo() { Address = addr, Type = SnesMemoryType.CpuMemory };
									absAddr = DebugApi.GetAbsoluteAddress(relAddr);
								}

								if(absAddr.Address < 0) {
									continue;
								}

								string orgLabel = label;
								int j = 1;
								while(_labelDefinitions.ContainsKey(label)) {
									label = orgLabel + j.ToString();
									j++;
								}

								_labelDefinitions[label] = new CodeLabel() {
									Label = label,
									Address = (UInt32)absAddr.Address,
									MemoryType = absAddr.Type,
									Comment = "",
									Flags = CodeLabelFlags.None,
									Length = 1
								};

								_sourceSymbols.Add(new SourceSymbol() { Name = label, Address = absAddr.Address, InternalSymbol = _labelDefinitions[label] });
								_labelNamesToSourceSymbols[label] = _sourceSymbols.Count - 1;
							}
						} else {
							break;
						}
					}
				} else if(str == "[source files]") {
					for(; i < lines.Length; i++) {
						if(lines[i].Length > 0) {
							Match m = fileRegex.Match(lines[i]);
							if(m.Success) {
								int fileId = Int32.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
								//int fileCrc = Int32.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
								string filePath = m.Groups[3].Value;

								string fullPath = Path.Combine(basePath, filePath);
								_sourceFiles[fileId] = new SourceFileInfo() {
									Name = filePath,
									Data = File.Exists(fullPath) ? File.ReadAllLines(fullPath) : new string[0]
								};
							}
						} else {
							break;
						}
					}
				} else if(str == "[addr-to-line mapping]") {
					for(; i < lines.Length; i++) {
						if(lines[i].Length > 0) {
							Match m = addrRegex.Match(lines[i]);
							if(m.Success) {
								int bank = Int32.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
								int addr = (bank << 16) | Int32.Parse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
								
								int fileId = Int32.Parse(m.Groups[3].Value, System.Globalization.NumberStyles.HexNumber);
								int lineNumber = Int32.Parse(m.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);

								if(lineNumber <= 0) {
									//Ignore line number 0, seems like bad data?
									//Line numbers in WLA symbol files should be 1-based.
									continue;
								}

								lineNumber -= 1;

								AddressInfo relAddr = new AddressInfo() { Address = addr, Type = SnesMemoryType.CpuMemory };
								AddressInfo absAddr = DebugApi.GetAbsoluteAddress(relAddr);
								_addressByLine[_sourceFiles[fileId].Name + "_" + lineNumber.ToString()] = absAddr;
								_linesByAddress[absAddr.Type.ToString() + absAddr.Address.ToString()] = new SourceCodeLocation() { File = _sourceFiles[fileId], LineNumber = lineNumber };
							}
						} else {
							break;
						}
					}
				}
			}

			LabelManager.SetLabels(_labelDefinitions.Values, true);
		}
	}
}
