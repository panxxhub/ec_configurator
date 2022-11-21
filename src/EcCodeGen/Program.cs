
using System.CommandLine;

// See https://aka.ms/new-console-template for more information

var command = new RootCommand();


var masterYamlConfigFilePathOption = new Option<FileInfo>(
   name: "--config",
   description: "master yaml config file path"
   )
{ IsRequired = true };
var outputDirectoryOption = new Option<DirectoryInfo>(
   name: "--output",
   description: "output directory",
   getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory())
   )
{ IsRequired = true };

masterYamlConfigFilePathOption.AddAlias("-c");
outputDirectoryOption.AddAlias("-o");

command.Add(masterYamlConfigFilePathOption);
command.Add(outputDirectoryOption);

command.SetHandler((configFile, outputDirectory) =>
{
	var master = EcCodeGen.MasterParser.Parse(configFile);

	// write to file ${outputDirectory}/msg.h
	using var h_stream = new FileStream(
		       Path.Combine(outputDirectory.FullName, "msg.h"),
		       FileMode.Create,
		       FileAccess.Write
		       );

	using var hpp_stream = new FileStream(
		       Path.Combine(outputDirectory.FullName, "msg.hpp"),
		       FileMode.Create,
		       FileAccess.Write
		       );
	using var slave_config = new FileStream(
		       Path.Combine(outputDirectory.FullName, "slave_config_impl.c"),
		       FileMode.Create,
		       FileAccess.Write
		       );
	using var slave_config_h = new FileStream(
		       Path.Combine(outputDirectory.FullName, "slave_config_impl.h"),
		       FileMode.Create,
		       FileAccess.Write
		       );



	using var writer = new StreamWriter(h_stream);
	using var hpp_writer = new StreamWriter(hpp_stream);
	using var slave_config_writer = new StreamWriter(slave_config);
	using var slave_config_h_writer = new StreamWriter(slave_config_h);

	writer.Write(master.MsgH);
	hpp_writer.Write(master.MsgHpp);
	slave_config_writer.Write(master.SlaveConfig);
	slave_config_h_writer.Write(master.SlaveConfigH);
}, masterYamlConfigFilePathOption, outputDirectoryOption);

await command.InvokeAsync(args);