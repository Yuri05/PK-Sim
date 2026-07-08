using OSPSuite.Core.Domain;
using OSPSuite.Utility.Validation;

namespace PKSim.CLI.Core.RunOptions
{
   public class DownloadModelsRunOptions : Notifier, IValidatable, IWithOutputFolder
   {
      private string _outputFolder;

      public string OutputFolder
      {
         get => _outputFolder;
         set => SetProperty(ref _outputFolder, value);
      }

      public IBusinessRuleSet Rules { get; } = new BusinessRuleSet();
   }
}
