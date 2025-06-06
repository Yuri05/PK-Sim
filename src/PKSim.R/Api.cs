﻿using System;
using OSPSuite.Utility.Container;
using OSPSuite.Utility.Extensions;
using PKSim.CLI.Core.RunOptions;
using PKSim.CLI.Core.Services;
using PKSim.R.Bootstrap;
using PKSim.R.Services;

namespace PKSim.R
{
   public static class Api
   {
      private static IContainer _container;

      public static void InitializeOnce()
      {
         try
         {
           _container = ApplicationStartup.Initialize();
         }
         catch (System.Reflection.ReflectionTypeLoadException e)
         {
            e.LoaderExceptions?.Each(x=>Console.WriteLine(e.FullMessage()));
            throw;
         }
      }

      public static IIndividualFactory GetIndividualFactory() => resolveTask<IIndividualFactory>();

      public static IPopulationFactory GetPopulationFactory() => resolveTask<IPopulationFactory>();

      public static void RunSnapshot(SnapshotRunOptions runOptions) => resolveTask<IBatchRunner<SnapshotRunOptions>>().RunBatchAsync(runOptions).Wait();

      public static void RunExport(ExportRunOptions runOptions) => resolveTask<IBatchRunner<ExportRunOptions>>().RunBatchAsync(runOptions).Wait();

      public static void RunQualification(QualificationRunOptions runOptions) => resolveTask<IBatchRunner<QualificationRunOptions>>().RunBatchAsync(runOptions).Wait();

      public static void RunJson(JsonRunOptions runOptions) => resolveTask<IBatchRunner<JsonRunOptions>>().RunBatchAsync(runOptions).Wait();

      public static void RunSimulationExport(ExportRunOptions runOptions) => resolveTask<IExportSimulationRunner>().RunBatchAsync(runOptions).Wait();

      private static T resolveTask<T>()
      {
         try
         {
            return _container.Resolve<T>();
         }
         catch (Exception e)
         {
            Console.WriteLine(e.FullMessage());
            throw;
         }
      }
   }
}