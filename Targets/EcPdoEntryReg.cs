namespace EcConf.Targets;


class EcPdoEntryReg
{
	public int Alias { get; set; }
	public int Position { get; set; }
	public int VendorId { get; set; }
	public int ProductCode { get; set; }

	public int Index { get; set; }
	public int SubIndex { get; set; }
	public string Offset { get; set; } = "&ignored";

}