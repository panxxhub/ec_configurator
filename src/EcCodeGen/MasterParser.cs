using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace EcCodeGen;

public static class MasterParser
{

	public static Master Parse(FileInfo yamlFile)
	{
		// read yaml file
		var yaml = File.ReadAllText(yamlFile.FullName);

		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();
		var master = deserializer.Deserialize<Master>(yaml);
		return master;
	}



}

// public static List<EcInfo.>