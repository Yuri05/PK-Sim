using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OSPSuite.Core.Services;

namespace PKSim.CLI.Core.Services
{
   public class ModelArchiveDownloader : IModelArchiveDownloader
   {
      private readonly IOSPSuiteLogger _logger;

      /// <summary>
      /// List of model URLs to download
      /// Format: (URL, FileName in archive)
      /// </summary>
      private readonly List<(string Url, string FileName)> _modelUrls = new List<(string, string)>
      {
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Alfentanil-Model/refs/heads/master/Alfentanil-Model.json", "Alfentanil-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Amikacin-Model/refs/heads/master/Amikacin-Model.json", "Amikacin-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Atomoxetine-Model/refs/heads/main/Atomoxetine-model.json", "Atomoxetine-model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Carbamazepine-Midazolam-DDI/refs/heads/main/Carbamazepine-Midazolam-DDI.json", "Carbamazepine-Midazolam-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Carbamazepine-Model/refs/heads/main/Carbamazepine-Model.json", "Carbamazepine-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Cimetidine-Verapamil-DDI/refs/heads/main/Cimetidine-Verapamil-DDI.json", "Cimetidine-Verapamil-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Clarithromycin-Dabigatran-DDI/refs/heads/main/Clarithromycin-Dabigatran-DDI.json", "Clarithromycin-Dabigatran-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Clarithromycin-Model/refs/heads/master/Clarithromycin-Model.json", "Clarithromycin-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Clomiphene-Model/refs/heads/main/(E)-clomiphene-DGI-Model.json", "(E)-clomiphene-DGI-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Desipramine-Model/refs/heads/main/Desipramine-Model.json", "Desipramine-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Dextromethorphan-Model/refs/heads/main/Dextromethorphan-model.json", "Dextromethorphan-model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Digoxin-Pediatrics/refs/heads/main/Digoxin_Pediatrics.json", "Digoxin_Pediatrics.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Erythromycin-Alprazolam-DDI/refs/heads/master/Erythromycin-Alprazolam-DDI.json", "Erythromycin-Alprazolam-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Erythromycin-Carbamazepine-DDI/refs/heads/main/Erythromycin-Carbamazepine-DDI.json", "Erythromycin-Carbamazepine-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Ethinylestradiol-Model/refs/heads/main/Ethinylestradiol-Model.json", "Ethinylestradiol-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Fluconazole-Model/refs/heads/main/Fluconazole-Model.json", "Fluconazole-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Itraconazole-Alprazolam-DDI/refs/heads/master/Itraconazole-Alprazolam-DDI.json", "Itraconazole-Alprazolam-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Itraconazole-Dabigatran-DDI/refs/heads/main/Itraconazole-Dabigatran-DDI.json", "Itraconazole-Dabigatran-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Levonorgestrel/refs/heads/main/Levonorgestrel-Model.json", "Levonorgestrel-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Lisinopril-model/refs/heads/master/Lisinopril-Model.json", "Lisinopril-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Mefenamic-acid-Model/refs/heads/master/Mefenamic_acid-Model.json", "Mefenamic_acid-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Mefenamic_acid-Dapagliflozin-DDI/refs/heads/master/Mefenamic_acid-Dapagliflozin-DDI.json", "Mefenamic_acid-Dapagliflozin-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Metoprolol-Model/refs/heads/main/Metoprolol-Model.json", "Metoprolol-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Mexiletine-Model/refs/heads/main/Mexiletine-Model.json", "Mexiletine-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Moclobemide-Model/refs/heads/main/Moclobemide-Model.json", "Moclobemide-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Montelukast-Model/refs/heads/master/Montelukast.json", "Montelukast.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Moxidectin-Model/refs/heads/master/Moxidectin.json", "Moxidectin.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Omeprazole-Model/refs/heads/main/Omeprazole-Model.json", "Omeprazole-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Pyrimethamine-Model/refs/heads/main/Pyrimethamine-Model.json", "Pyrimethamine-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Raltegravir-Model/refs/heads/master/Raltegravir-Model.json", "Raltegravir-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Rifampicin-Dabigatran-DDI/refs/heads/main/Rifampicin-Dabigatran-DDI.json", "Rifampicin-Dabigatran-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Rifampicin-Verapamil-DDI/refs/heads/main/Rifampicin-Verapamil-DDI.json", "Rifampicin-Verapamil-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Risperidone-Model/refs/heads/main/Risperidone-model.json", "Risperidone-model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/S-Mephenytoin-Model/refs/heads/main/S_Mephenytoin-Model.json", "S_Mephenytoin-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Sufentanil-Model/refs/heads/master/Sufentanil.json", "Sufentanil.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Tizanidine-Model/refs/heads/main/Tizanidine-Model.json", "Tizanidine-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Triazolam-Model/refs/heads/master/Triazolam-Model.json", "Triazolam-Model.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Vancomycin-Model/refs/heads/master/Vancomycin.json", "Vancomycin.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Verapamil-Dabigatran-DDI/refs/heads/main/Verapamil-Dabigatran-DDI.json", "Verapamil-Dabigatran-DDI.json"),
         ("https://raw.githubusercontent.com/Open-Systems-Pharmacology/Warfarin-Model/refs/heads/main/Warfarin-Model.json", "Warfarin-Model.json"),
      };

      public ModelArchiveDownloader(IOSPSuiteLogger logger)
      {
         _logger = logger;
      }

      public IEnumerable<(string Url, string FileName)> GetModelUrls()
      {
         return _modelUrls;
      }

      public async Task CreateArchiveAsync(string outputPath)
      {
         _logger.AddInfo($"Starting to create model archive at {outputPath}");

         var tempDirectory = Path.Combine(Path.GetTempPath(), "PK-Sim-Models-" + Guid.NewGuid());
         Directory.CreateDirectory(tempDirectory);

         try
         {
            // Create a temporary directory to download files
            _logger.AddInfo($"Downloading {_modelUrls.Count} model files...");

            using (var httpClient = new HttpClient())
            {
               int downloadedCount = 0;
               int failedCount = 0;

               foreach (var (url, fileName) in _modelUrls)
               {
                  try
                  {
                     _logger.AddDebug($"Downloading {fileName} from {url}");
                     var content = await httpClient.GetByteArrayAsync(url);
                     var filePath = Path.Combine(tempDirectory, fileName);
                     await File.WriteAllBytesAsync(filePath, content);
                     downloadedCount++;
                     _logger.AddDebug($"Successfully downloaded {fileName}");
                  }
                  catch (Exception ex)
                  {
                     failedCount++;
                     _logger.AddWarning($"Failed to download {fileName}: {ex.Message}");
                  }
               }

               _logger.AddInfo($"Downloaded {downloadedCount} files successfully, {failedCount} files failed");
            }

            // Create the archive
            _logger.AddInfo($"Creating archive...");
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
               Directory.CreateDirectory(outputDirectory);
            }

            // Remove existing archive if it exists
            if (File.Exists(outputPath))
            {
               File.Delete(outputPath);
            }

            ZipFile.CreateFromDirectory(tempDirectory, outputPath);
            _logger.AddInfo($"Archive created successfully at {outputPath}");
         }
         finally
         {
            // Clean up temporary directory
            try
            {
               if (Directory.Exists(tempDirectory))
               {
                  Directory.Delete(tempDirectory, true);
               }
            }
            catch (Exception ex)
            {
               _logger.AddWarning($"Failed to clean up temporary directory: {ex.Message}");
            }
         }
      }
   }
}
