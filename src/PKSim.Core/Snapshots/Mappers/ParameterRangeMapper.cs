﻿using System.Threading.Tasks;
using SnapshotParameterRange = PKSim.Core.Snapshots.ParameterRange;
using ModelParameterRange = PKSim.Core.Model.ParameterRange;

namespace PKSim.Core.Snapshots.Mappers
{
   public class ParameterRangeSnapshotContext : SnapshotContext
   {
      public ModelParameterRange ParameterRange { get; }

      public ParameterRangeSnapshotContext(ModelParameterRange parameterRange, SnapshotContext baseContext) : base(baseContext)
      {
         ParameterRange = parameterRange;
      }
   }

   public class ParameterRangeMapper : SnapshotMapperBase<ModelParameterRange, SnapshotParameterRange, ParameterRangeSnapshotContext>
   {
      public override async Task<SnapshotParameterRange> MapToSnapshot(ModelParameterRange parameterRange)
      {
         if (parameterRange == null)
            return null;

         //No range defined. No need to save this range
         if (parameterRange.MaxValueInDisplayUnit == null && parameterRange.MaxValueInDisplayUnit == null)
            return null;

         return await SnapshotFrom(parameterRange, snapshot =>
         {
            snapshot.Min = parameterRange.MinValueInDisplayUnit;
            snapshot.Max = parameterRange.MaxValueInDisplayUnit;
            snapshot.Unit = parameterRange.Unit.Name;
         });
      }

      public override Task<ModelParameterRange> MapToModel(SnapshotParameterRange snapshot, ParameterRangeSnapshotContext snapshotContext)
      {
         var parameterRange = snapshotContext.ParameterRange;

         //Range not available in population or snapshot range not defined. Nothing to do
         if (parameterRange == null || snapshot == null)
            return Task.FromResult(parameterRange);

         parameterRange.Unit = parameterRange.Dimension.Unit(ModelValueFor(snapshot.Unit));
         parameterRange.MaxValueInDisplayUnit = snapshot.Max;
         parameterRange.MinValueInDisplayUnit = snapshot.Min;

         return Task.FromResult(parameterRange);
      }
   }
}