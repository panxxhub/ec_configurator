namespace EcCodeGen;

public class EcPdoEntryRegT
{
	public int Alias { get; set; }
	public int Position { get; set; }
	public int ProductCode { get; set; }
	public int Index { get; set; }
	public int SubIndex { get; set; }
	public EcVariable OffsetRef { get; set; }

	public string Entry
	{
		get
		{
			return $"{{.alias={Alias}, .position={Position}, .product_code=0x{ProductCode:X4}, .index=0x{Index:X4}, .sub_index={SubIndex}, .offset=&{OffsetRef.Name}, .bit_position=NULL}}";
		}


	}

}
public class EcPdoEntryInfoT
{
	public int Index { get; set; }
}