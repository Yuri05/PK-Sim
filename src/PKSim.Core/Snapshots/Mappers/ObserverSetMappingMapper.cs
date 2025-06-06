﻿using System.Threading.Tasks;
using OSPSuite.Core.Services;
using PKSim.Assets;
using PKSim.Core.Model;

namespace PKSim.Core.Snapshots.Mappers
{
   public class ObserverSetMappingMapper : SnapshotMapperBase<ObserverSetMapping, ObserverSetSelection, SnapshotContext, PKSimProject>
   {
      private readonly IOSPSuiteLogger _logger;

      public ObserverSetMappingMapper(IOSPSuiteLogger logger)
      {
         _logger = logger;
      }

      public override Task<ObserverSetSelection> MapToSnapshot(ObserverSetMapping observerSetMapping, PKSimProject project)
      {
         var observerSetBuildingBlock = project.BuildingBlockById(observerSetMapping.TemplateObserverSetId);
         return SnapshotFrom(observerSetMapping, x => { x.Name = observerSetBuildingBlock.Name; });
      }

      public override Task<ObserverSetMapping> MapToModel(ObserverSetSelection snapshot, SnapshotContext snapshotContextContext)
      {
         var observerSet = snapshotContextContext.Project.BuildingBlockByName<Model.ObserverSet>(snapshot.Name);
         if (observerSet == null)
         {
            _logger.AddError(PKSimConstants.Error.CannotFindObserverSetForMapping(snapshot.Name));
            return null;
         }

         var observerSetMapping = new ObserverSetMapping {TemplateObserverSetId = observerSet.Id ?? string.Empty};
         return Task.FromResult(observerSetMapping);
      }
   }
}