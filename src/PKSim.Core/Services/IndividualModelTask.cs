using System.Linq;
using OSPSuite.Core.Domain;
using OSPSuite.Utility.Extensions;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using static PKSim.Core.CoreConstants.Parameters;

namespace PKSim.Core.Services
{
   public interface IIndividualModelTask
   {
      void CreateModelFor(Individual individual);
      /// <summary>
      /// Creates the model structure (containers and neighborhoods)
      /// </summary>
      /// <param name="individual"></param>
      void CreateModelStructureFor(Individual individual);

      /// <summary>
      /// Creates the organ structure (containers only)
      /// </summary>
      /// <param name="individual"></param>
      void CreateOrganStructureFor(Individual individual);
      
      IParameter MeanAgeFor(OriginData originData);
      IParameter MeanGestationalAgeFor(OriginData originData);
      IParameter MeanWeightFor(OriginData originData);
      IParameter MeanHeightFor(OriginData originData);
      IParameter MeanOrganismParameter(OriginData originData, string parameterName);

      /// <summary>
      ///    Returns a BMI Parameter as a function of height and weight
      /// </summary>
      IParameter BMIBasedOn(OriginData originData, IParameter parameterWeight, IParameter parameterHeight);

   }

   public class IndividualModelTask : IIndividualModelTask
   {
      private readonly IParameterContainerTask _parameterContainerTask;
      private readonly ISpeciesContainerQuery _speciesContainerQuery;
      private readonly IBuildingBlockFinalizer _buildingBlockFinalizer;
      private readonly IFormulaFactory _formulaFactory;
      private readonly IPopulationAgeRepository _populationAgeRepository;

      public IndividualModelTask(
         IParameterContainerTask parameterContainerTask, 
         ISpeciesContainerQuery speciesContainerQuery,
         IBuildingBlockFinalizer buildingBlockFinalizer, 
         IFormulaFactory formulaFactory,
         IPopulationAgeRepository populationAgeRepository)
      {
         _parameterContainerTask = parameterContainerTask;
         _speciesContainerQuery = speciesContainerQuery;
         _buildingBlockFinalizer = buildingBlockFinalizer;
         _formulaFactory = formulaFactory;
         _populationAgeRepository = populationAgeRepository;
      }

      public void CreateModelFor(Individual individual)
      {
         var organism = individual.Organism;
         var originData = individual.OriginData;

         addModelStructureTo(organism, originData, addParameter: true);

         setAgeSettings(organism.Parameter(AGE), originData.Population.Name, setValueAndDisplayUnit: false);

         addWeightParameterTags(individual);

         addModelStructureTo(individual.Neighborhoods, originData, addParameter: true);

         _buildingBlockFinalizer.Finalize(individual);
      }

      public void CreateOrganStructureFor(Individual individual)
      {
         addModelStructureTo(individual.Organism, individual.OriginData, addParameter: false);
      }

      public void CreateModelStructureFor(Individual individual)
      {
         addModelStructureTo(individual.Organism, individual.OriginData, addParameter: false);
         addModelStructureTo(individual.Neighborhoods, individual.OriginData, addParameter: false);
      }

      public IParameter MeanAgeFor(OriginData originData)
      {
         var ageParameter = MeanOrganismParameter(originData, AGE);
         setAgeSettings(ageParameter, originData.Population.Name, setValueAndDisplayUnit: true);

         return ageParameter;
      }

      //workaround for body weight sum formula.
      //need to find a better solution
      private void addWeightParameterTags(Individual individual)
      {
         var allOrganWeightParameters = individual.Organism.GetAllChildren<Parameter>
            (p => p.Name.Equals(WEIGHT_TISSUE)).ToList();

         allOrganWeightParameters.Each(addParentTagsTo);
      }

      private void addParentTagsTo(Parameter parameter)
      {
         var parentContainer = parameter?.ParentContainer;
         parentContainer?.Tags.Each(t => parameter.AddTag(t.Value));
      }

      private void setAgeSettings(IParameter ageParameter, string population, bool setValueAndDisplayUnit)
      {
         if (ageParameter == null)
            return;

         var populationAgeSettings = _populationAgeRepository.PopulationAgeSettingsFrom(population);

         ageParameter.MinValue = populationAgeSettings.MinAge;
         ageParameter.MaxValue = populationAgeSettings.MaxAge;

         if (!setValueAndDisplayUnit)
            return;

         ageParameter.Value = populationAgeSettings.DefaultAge;
         ageParameter.DisplayUnit = ageParameter.Dimension.UnitOrDefault(populationAgeSettings.DefaultAgeUnit);
      }

      public IParameter MeanGestationalAgeFor(OriginData originData)
      {
         var param = MeanOrganismParameter(originData, Constants.Parameters.GESTATIONAL_AGE);
         //for population not preterm where the parameter is actually defined, the value of the parameter should be set to another default
         if (param != null && !originData.Population.IsPreterm)
            param.Value = CoreConstants.NOT_PRETERM_GESTATIONAL_AGE_IN_WEEKS;

         return param;
      }

      public IParameter MeanWeightFor(OriginData originData) => MeanOrganismParameter(originData, MEAN_WEIGHT);

      public IParameter MeanHeightFor(OriginData originData) => MeanOrganismParameter(originData, MEAN_HEIGHT);

      public IParameter MeanOrganismParameter(OriginData originData, string parameterName)
      {
         var organism = new Organism().WithName(Constants.ORGANISM);
         _parameterContainerTask.AddIndividualParametersTo(organism, originData, parameterName);
         return organism.Parameter(parameterName);
      }

      public IParameter BMIBasedOn(OriginData originData, IParameter parameterWeight, IParameter parameterHeight)
      {
         var standardBMI = MeanOrganismParameter(originData, BMI);
         if (standardBMI == null) 
            return null;

         var organism = new Container().WithName(Constants.ORGANISM);
         organism.AddChildren(parameterWeight, parameterHeight, standardBMI);

         standardBMI.Formula = _formulaFactory.BMIFormulaFor(parameterWeight, parameterHeight);
         standardBMI.Formula.ResolveObjectPathsFor(standardBMI);
         return standardBMI;
      }

      private void addModelStructureTo(IContainer container, OriginData originData, bool addParameter)
      {
         if (addParameter)
            _parameterContainerTask.AddIndividualParametersTo(container, originData);

         foreach (var subContainer in _speciesContainerQuery.SubContainersFor(originData.Population, container))
         {
            container.Add(subContainer);
            addModelStructureTo(subContainer, originData, addParameter);
         }
      }
   }
}