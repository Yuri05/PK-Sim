using System.Collections.Generic;
using System.Text;
using CommandLine;
using CommandLine.Text;
using OSPSuite.Utility.Extensions;
using PKSim.CLI.Core.RunOptions;

namespace PKSim.CLI.Commands
{
   [Verb("download-models", HelpText = "Download OSP model files and create an archive.")]
   public class DownloadModelsCommand : CLICommand<DownloadModelsRunOptions>, IWithOutputFolder
   {
      public override string Name { get; } = "Download Models";

      [Option('o', "output", Required = true, HelpText = "Output folder where the archive will be saved.")]
      public string OutputFolder { get; set; }

      [Usage(ApplicationAlias = "PKSim.CLI")]
      public static IEnumerable<Example> Examples
      {
         get
         {
            var downloadModels = new DownloadModelsCommand { OutputFolder = "<OutputFolder>" };
            yield return new Example("Download all OSP models and create an archive", UnParserSettings.WithGroupSwitchesOnly(), downloadModels);
            yield return new Example("Download all OSP models and create an archive (long notation)", new UnParserSettings(), downloadModels);
         }
      }

      public override string ToString()
      {
         var sb = new StringBuilder();
         this.LogOutputFolder(sb);
         LogDefaultOptions(sb);
         return sb.ToString();
      }

      public override DownloadModelsRunOptions ToRunOptions()
      {
         return new DownloadModelsRunOptions
         {
            OutputFolder = OutputFolder,
         };
      }
   }
}
