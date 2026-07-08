using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKSim.CLI.Core.Services
{
   public interface IModelArchiveDownloader
   {
      /// <summary>
      /// Downloads model files from the specified URLs and creates an archive
      /// </summary>
      /// <param name="outputPath">Full path where the archive should be saved</param>
      /// <returns>A task representing the asynchronous operation</returns>
      Task CreateArchiveAsync(string outputPath);

      /// <summary>
      /// Gets the list of model URLs to download
      /// </summary>
      IEnumerable<(string Url, string FileName)> GetModelUrls();
   }
}
