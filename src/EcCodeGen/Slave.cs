using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace EcCodeGen;
public enum SlaveType : int
{

	DigitalIO = 0,
	AcServo = 1,
}

public class Slave
{
	public int GroupIndex { get; set; }
	public uint ProductCode { get; set; }
	public uint VendorId { get; set; }
	public int Index { get; set; }
	public string EsiFile { get; set; }
	public SlaveType Type { get; set; }
	public bool ConfigPdo { get; set; }
	public bool DcEnable { get; set; }

	[NotMapped]
	private EcInfo.EtherCATInfo EcInfo
	{
		get
		{
			var serializer = new XmlSerializer(typeof(EcInfo.EtherCATInfo));
			using var stream = new FileStream(EsiFile, FileMode.Open);
			var config = serializer.Deserialize(stream) as EcInfo.EtherCATInfo;
			return config;
		}
	}
	[NotMapped]
	public EcInfo.EtherCATInfoDescriptionsDevicesDevice EcDevice => EcInfo.Descriptions.Devices.Single(x => ProductCode == x.Type.ProductCode.ResolveEcNumber());
	[NotMapped]
	public IEnumerable<EcBase.PdoType> TxPdos => EcDevice.TxPdo.Where(x => x.SmSpecified);
	[NotMapped]
	public IEnumerable<EcBase.PdoType> RxPdos => EcDevice.RxPdo.Where(x => x.SmSpecified);


	[NotMapped]
	public string RxPdoContent => RxPdos.StructPdo(true, false);
	[NotMapped]
	public string TxPdoContent => TxPdos.StructPdo(false, false);

	[NotMapped]
	public int TxPdoSize => TxPdos.SelectMany(x => x.Entry).Select(x => x.BitLen).Sum();
	[NotMapped]
	public int RxPdoSize => RxPdos.SelectMany(x => x.Entry).Select(x => x.BitLen).Sum();

	[NotMapped]
	public string SyncGroupName { get; set; }

	[NotMapped]
	public int SmCount => TxPdos.Concat(RxPdos).Select(x => x.Sm).Distinct().Count();
	public string ConfigName => $"slave_config_{Index}";

	public string SyncConfig(string sub_group_name, out List<string> variables)
	{
		var rx_sm_lookup = RxPdos.ToLookup(x => x.Sm);
		var tx_sm_lookup = TxPdos.ToLookup(x => x.Sm);
		var entries = new List<string>();
		var pdo_infos = new List<string>();
		var entries_list = new List<string>();
		variables = new List<string>();

		foreach (var element in rx_sm_lookup.Select((sm, idx) => new { sm, idx }))
		{

			var pdo_info_list_name = $"{sub_group_name}_sm{element.sm.Key}_{element.idx}_pdos";
			var sync_info = new Dictionary<string, string>
			{
				["index"] = $"{element.sm.Key}",
				["dir"] = "EC_DIR_OUTPUT",
				["n_pdos"] = $"{element.sm.Count()}",
				["pdos"] = pdo_info_list_name,
				["watchdog_mode"] = Type == SlaveType.AcServo ? "EC_WD_ENABLE" : "EC_WD_DEFAULT"
			};


			var pdo_info_list = element.sm.Select((pdo, pdo_idx) =>
			{
				var entries_info_name = $"{pdo_info_list_name}_{pdo_idx}_entries";

				var entries_info = pdo.Entry.Select(entry =>
				{
					var entry_info_dict = new Dictionary<string, string>
					{
						["index"] = $"0x{entry.Index.Value.ResolveEcNumber():X4}",
						["subindex"] = $"{entry.SubIndex.ResolveEcNumber()}",
						["bit_length"] = $"{entry.BitLen}"
					};
					return entry_info_dict.MakeString(true);
				});
				var entries_str = $@"
static ec_pdo_entry_info_t {entries_info_name}[] = {{
{string.Join(",\n", entries_info)}
}};
";
				entries_list.Add(entries_str);

				var ec_pdo_info = new Dictionary<string, string>
				{
					["index"] = $"0x{pdo.Index.Value.ResolveEcNumber():X4}",
					["n_entries"] = $"{pdo.Entry.Count}",
					["entries"] = entries_info_name,
				};
				return ec_pdo_info.MakeString(true);
			});


			var pdo_info = $@"
static ec_pdo_info_t {pdo_info_list_name}[] = {{
{string.Join(",\n", pdo_info_list)}
}};
";
			pdo_infos.Add(pdo_info);
			entries.Add(sync_info.MakeString(true));
		}

		foreach (var element in tx_sm_lookup.Select((sm, idx) => new { sm, idx }))
		{

			var pdo_info_list_name = $"{sub_group_name}_sm{element.sm.Key}_{element.idx}_pdos";

			var sync_info = new Dictionary<string, string>
			{
				["index"] = $"{element.sm.Key}",
				["dir"] = "EC_DIR_INPUT",
				["n_pdos"] = $"{element.sm.Count()}",
				["pdos"] = pdo_info_list_name,
				["watchdog_mode"] = Type == SlaveType.AcServo ? "EC_WD_ENABLE" : "EC_WD_DEFAULT"
			};


			var pdo_info_list = element.sm.Select((pdo, pdo_idx) =>
			{
				var entries_info_name = $"{pdo_info_list_name}_{pdo_idx}_entries";

				var entries_info = pdo.Entry.Select(entry =>
				{
					var entry_info_dict = new Dictionary<string, string>
					{
						["index"] = $"0x{entry.Index.Value.ResolveEcNumber():X4}",
						["subindex"] = $"{entry.SubIndex.ResolveEcNumber()}",
						["bit_length"] = $"{entry.BitLen}"
					};
					return entry_info_dict.MakeString(true);
				});
				var entries_str = $@"
static ec_pdo_entry_info_t {entries_info_name}[] = {{
{string.Join(",\n", entries_info)}
}};
";
				entries_list.Add(entries_str);

				var ec_pdo_info = new Dictionary<string, string>
				{
					["index"] = $"0x{pdo.Index.Value.ResolveEcNumber():X4}",
					["n_entries"] = $"{pdo.Entry.Count}",
					["entries"] = entries_info_name,
				};
				return ec_pdo_info.MakeString(true);
			});


			var pdo_info = $@"
static ec_pdo_info_t {pdo_info_list_name}[] = {{
{string.Join(",\n", pdo_info_list)}
}};
";
			pdo_infos.Add(pdo_info);

			entries.Add(sync_info.MakeString(true));

		}
		var entries_str = $@"
const ec_sync_info_t {sub_group_name}_sync_info[] = {{
{string.Join(",\n", entries)},
{{0xFF}},
}};
";
		// this.SyncGroupName = $"{sub_group_name}_sync_info";

		variables.Add($"ec_sync_info_t {sub_group_name}_sync_info[]");

		return entries_list.Concat(pdo_infos).Aggregate((a, b) => a + "\n" + b) + "\n" + entries_str;

	}

	public string AppConfigSlaveSnippet
	{
		get
		{

			var snippet_pdo = $@"
	if(config_idx == {Index}) {{
		{ConfigName} = ecrt_master_slave_config(master,0,{Index},0x{VendorId:X8},0x{ProductCode:X8});
		if({ConfigName} == NULL) {{
			return -1;
		}}
		if(ecrt_slave_config_pdos({ConfigName},{SmCount},{SyncGroupName}) != 0) {{
			return -1;
		}}
	}}
";

			var snippet_no_pdo = $@"
	if(config_idx == {Index}) {{
		{ConfigName} = ecrt_master_slave_config(master,0,{Index},0x{VendorId:X8},0x{ProductCode:X8});
		if({ConfigName} == NULL) {{
			return -1;
		}}
	}}
";
			return ConfigPdo ? snippet_pdo : snippet_no_pdo;
		}

	}

	public string AppEtherConfigDcSnippet
	{
		get
		{
			var snippet_dc_en = $@"
        ecrt_slave_config_dc({ConfigName}, EC_DC_ACTIVATE, EC_DC_CYCLE_TIME, 0, 0, 0);";
			return DcEnable ? snippet_dc_en : "";

		}
	}


}