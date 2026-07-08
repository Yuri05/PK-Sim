using System.Threading.Tasks;
using OSPSuite.Core.Services;
using PKSim.CLI.Core.RunOptions;

namespace PKSim.CLI.Core.Services
{
   public interface IModelDownloadBatchRunner : IBatchRunner<DownloadModelsRunOptions>
   {
   }

   public class ModelDownloadBatchRunner : IModelDownloadBatchRunner
   {
      private readonly IModelArchiveDownloader _archiveDownloader;
      private readonly IOSPSuiteLogger _logger;

      public ModelDownloadBatchRunner(IModelArchiveDownloader archiveDownloader, IOSPSuiteLogger logger)
      {
         _archiveDownloader = archiveDownloader;
         _logger = logger;
      }

      public async Task RunBatchAsync(DownloadModelsRunOptions runOptions)
      {
         _logger.AddInfo("Starting model archive download batch");
         
         if (string.IsNullOrWhiteSpace(runOptions.OutputFolder))
         {
            throw new InvalidOperationException($"Output folder must be specified ('{nameof(runOptions.OutputFolder)}' parameter is null or empty)");
         }

         var archivePath = System.IO.Path.Combine(runOptions.OutputFolder, "OSP-Models.zip");
         await _archiveDownloader.CreateArchiveAsync(archivePath);

         _logger.AddInfo("Model archive download batch completed successfully");
      }
   }
}
