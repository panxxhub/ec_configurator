using System.Text.RegularExpressions;

namespace EcCodeGen;

public static class EcExtensions
{
	public static uint ResolveEcNumber(this string v)
	{
		Regex hexPattern = new(@"(?<=#x)[0-9a-fA-F]{1,}$", RegexOptions.Compiled);
		Regex decPattern = new(@"([+-]?[0-9]{1,})", RegexOptions.Compiled);
		if (hexPattern.IsMatch(v))
		{
			var hex = hexPattern.Match(v).Groups[0].Value;
			return uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);
		}
		else if (decPattern.IsMatch(v))
		{
			var dec = decPattern.Match(v).Groups[0].Value;
			return uint.Parse(dec);
		}
		throw new System.Exception($"Invalid EcNumber: {v}");
	}
	public static string EcPdoEntryInfo(this EcBase.PdoTypeEntry entry)
	{
		// entry.Index.Value;
		var index = entry.Index.Value.ResolveEcNumber();
		// var subindex = entry.SubIndex
		var subIndex = entry.SubIndex.ResolveEcNumber();
		var bit_len = entry.BitLen;

		return $@"{{ .index = 0x{index:X4}, .subindex = {subIndex}, .bit_length = {bit_len} }},";
	}
	public static List<string> EcPdoEntryRegs(this IEnumerable<Slave> slaves, bool rx, string group_name)
	{
		var regs = new List<string>();

		foreach (var slave in slaves)
		{
			foreach (var pdo in (rx ? slave.RxPdos : slave.TxPdos))
			{
				foreach (var entry in pdo.Entry)
				{
					var index = entry.Index.Value.ResolveEcNumber();
					var subIndex = entry.SubIndex.ResolveEcNumber();
					var offset = regs.Any() ? "ignored" : $"{group_name}_head";
					string reg_str =
					$@"{{ .alias = 0, .position = {slave.Index}, .vendor_id = 0x{slave.VendorId:X8}, .product_code = 0x{slave.ProductCode:X8}, .index = 0x{index:X4}, .subindex = {subIndex}, .offset = &{offset}, .bit_position = NULL }},";
					regs.Add(reg_str + "\n");
				}
			}
		}
		regs.Add("{},\n");
		return regs;
	}
	public static string EcSyncInfo(this EcBase.PdoType pdo, string pdo_set_name, bool rx, bool servo = false)
	{
		if (pdo.EntrySpecified == false)
			throw new System.Exception("pdo.Entry is not specified");
		if (pdo.SmSpecified == false)
			throw new System.Exception("pdo.Sm is not specified");
		var index = pdo.Sm;
		string dir = rx ? "EC_DIR_OUTPUT" : "EC_DIR_INPUT";
		var n_pdos = pdo.Entry.Count;
		var wd_mode = servo ? "EC_WD_ENABLE" : "EC_WD_DEFAULT";

		return $@"
\t{{
\t	.index = {index},
\t	.direction = {dir},
\t	.n_pdos = {n_pdos},
\t	.pdos = {pdo_set_name},
\t	.watchdog_mode = {wd_mode},
\t}},\n";
	}
	public static string NameSnakeCase(this EcBase.PdoTypeEntry entry)
	{
		// replace space in entry.Name with underscore
		var name = entry.Name.Single().Value;
		name = name.Replace(" ", "_").ToLower();
		Regex regex = new(@"([a-zA-Z0-9_\s]+)", RegexOptions.Compiled);
		// retrieve all words in entry.Name
		var clean_name = regex.Match(name).Groups[0].Value;

		// escape all numbers at the beginning and ending of the word
		clean_name = Regex.Replace(clean_name, @"^\d+", @"\$0");
		clean_name = Regex.Replace(clean_name, @"\d+$", @"\$0");

		return clean_name;

	}
	public static string StructPdo(this IEnumerable<EcBase.PdoType> pdos, bool rx, bool with_wrapper = true)
	{
		const string snippet_rx = "struct pdo_a2r_servo {\n";
		const string snippet_tx = "struct pdo_r2a_servo {\n";
		string v = pdos.SelectMany(x => x.Entry).Select(x => x.StructPdoElement()).GroupBy(x => x)
			.Select(x => x.Count() > 1 ? $"\t{x.Key}[{x.Count()}];\n" : $"\t{x.Key};\n")
			.Aggregate((a, b) => a + b);

		const string snippet_1 = "};\n";
		if (with_wrapper)
			return (rx ? snippet_rx : snippet_tx) + v + snippet_1;
		return v;
	}
	public static string StructPdoElement(this EcBase.PdoTypeEntry entry)
	{
		var name = entry.NameSnakeCase();
		return $"{entry.TypeName()} {name}";
	}
	public static string TypeName(this EcBase.PdoTypeEntry type)
	{

		return type.DataType.Value switch
		{
			"USINT" => $"uint{type.BitLen}_t",
			"SINT" => $"int{type.BitLen}_t",

			"INT" => $"int{type.BitLen}_t",
			"UINT" => $"uint{type.BitLen}_t",

			"DINT" => $"int{type.BitLen}_t",
			"UDINT" => $"uint{type.BitLen}_t",

			"BITARR8" => $"uint{type.BitLen}_t",

			_ => type.DataType.Value
		};
	}

	public static string MakeString(this Dictionary<string, string> dict, bool one_line = true)
	{
		if (one_line)
		{
			var content = dict.Select(x => $".{x.Key} = {x.Value}").Aggregate((a, b) => a + "," + b);
			return $"{{ {content} }}";
		}
		var content_multi_line = dict.Select(x => $"\t.{x.Key} = {x.Value}").Aggregate((a, b) => a + "\n" + b);
		return $"{{\n{content_multi_line}\n}}";
	}


}

// public static List<EcInfo.>