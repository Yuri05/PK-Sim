﻿using System.Threading.Tasks;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Formulas;
using PKSim.Assets;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;

namespace PKSim.Core.Snapshots.Mappers
{
   public class AlternativeMapperSnapshotContext : SnapshotContext
   {
      public ParameterAlternativeGroup ParameterAlternativeGroup { get; }

      public AlternativeMapperSnapshotContext(ParameterAlternativeGroup parameterAlternativeGroup, SnapshotContext baseContext) : base(baseContext)
      {
         ParameterAlternativeGroup = parameterAlternativeGroup;
      }
   }

   public class AlternativeMapper : ParameterContainerSnapshotMapperBase<ParameterAlternative, Alternative, AlternativeMapperSnapshotContext>
   {
      private const bool DEFAULT_IS_DEFAULT = true;

      private readonly ISpeciesRepository _speciesRepository;
      private readonly IParameterAlternativeFactory _parameterAlternativeFactory;
      private readonly ICompoundAlternativeTask _compoundAlternativeTask;

      public AlternativeMapper(ParameterMapper parameterMapper,
         IParameterAlternativeFactory parameterAlternativeFactory,
         ICompoundAlternativeTask compoundAlternativeTask,
         ISpeciesRepository speciesRepository) : base(parameterMapper)
      {
         _speciesRepository = speciesRepository;
         _parameterAlternativeFactory = parameterAlternativeFactory;
         _compoundAlternativeTask = compoundAlternativeTask;
      }

      public override async Task<Alternative> MapToSnapshot(ParameterAlternative parameterAlternative)
      {
         if (parameterAlternative.IsCalculated)
            return null;

         var snapshot = await SnapshotFrom(parameterAlternative, x =>
         {
            x.IsDefault = SnapshotValueFor(parameterAlternative.IsDefault, DEFAULT_IS_DEFAULT);
            x.Species = (parameterAlternative as ParameterAlternativeWithSpecies)?.Species.Name;
         });

         return snapshot;
      }

      public override async Task<ParameterAlternative> MapToModel(Alternative snapshot, AlternativeMapperSnapshotContext snapshotContext)
      {
         var parameterAlternativeGroup = snapshotContext.ParameterAlternativeGroup;
         var alternative = _parameterAlternativeFactory.CreateAlternativeFor(parameterAlternativeGroup);
         alternative.IsDefault = ModelValueFor(snapshot.IsDefault, DEFAULT_IS_DEFAULT);
         MapSnapshotPropertiesToModel(snapshot, alternative);

         await UpdateParametersFromSnapshot(snapshot, alternative, snapshotContext, parameterAlternativeGroup.Name);

         if (parameterAlternativeGroup.IsNamed(CoreConstants.Groups.COMPOUND_SOLUBILITY))
            updateSolubilityAlternative(alternative);

         var alternativeWithSpecies = alternative as ParameterAlternativeWithSpecies;
         if (alternativeWithSpecies == null)
            return alternative;

         alternativeWithSpecies.Species = _speciesRepository.FindByName(snapshot.Species);
         if (alternativeWithSpecies.Species == null)
            throw new SnapshotOutdatedException(PKSimConstants.Error.CouldNotFindSpecies(snapshot.Species, _speciesRepository.AllNames()));

         return alternativeWithSpecies;
      }

      private void updateSolubilityAlternative(ParameterAlternative solubilityAlternative)
      {
         var solubilityTable = solubilityAlternative.Parameter(CoreConstants.Parameters.SOLUBILITY_TABLE);
         //default structure, nothing to change
         if (!solubilityTable.Formula.IsTable())
            return;

         _compoundAlternativeTask.PrepareSolubilityAlternativeForTableSolubility(solubilityAlternative);
      }
   }
}