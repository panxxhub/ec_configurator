namespace EcConf.Targets;

class EcSyncInfo
{
	public int Index { get; set; }
	public bool Input { get; set; }
	public int NumberOfPdos { get; set; }
	public string WatchdogMode { get; set; } = "EC_WD_ENABLE";
}

