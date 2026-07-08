using OSPSuite.Utility.Container;
using PKSim.CLI.Core.RunOptions;
using PKSim.CLI.Core.Services;
using PKSim.Core;

namespace PKSim.CLI.Core
{
   public class CLIRegister : Register
   {
      public override void RegisterInContainer(IContainer container)
      {
         container.AddScanner(x =>
         {
            x.AssemblyContainingType<CLIRegister>();

            //Register services
            x.IncludeNamespaceContainingType<SnapshotRunner>();


            x.WithConvention<PKSimRegistrationConvention>();
         });

         //special registration that does not follow conventions
         container.Register<IBatchRunner<ExportRunOptions>, ExportSimulationRunner>();
         container.Register<IBatchRunner<DownloadModelsRunOptions>, ModelDownloadBatchRunner>();
         // HttpClient registered as singleton is acceptable for CLI applications that don't require connection pooling management
         // For server applications, IHttpClientFactory should be used instead
         container.Register<HttpClient>(new HttpClient());
      }
   }
}