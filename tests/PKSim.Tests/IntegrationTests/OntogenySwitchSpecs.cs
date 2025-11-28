using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Utility.Container;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using PKSim.Infrastructure;

namespace PKSim.IntegrationTests
{
   public abstract class concern_for_OntogenySwitch : ContextForSimulationIntegration<IBuildingBlockParametersToSimulationUpdater>
   {
      protected Individual _individual;
      protected Compound _compound;
      protected Protocol _protocol;
      protected ExpressionProfile _expressionProfile;
      protected IMoleculeExpressionTask<Individual> _moleculeExpressionTask;
      protected IOntogenyRepository _ontogenyRepository;
      protected IExpressionProfileUpdater _expressionProfileUpdater;
      protected ICoreWorkspace _workspace;
      protected const string ENZYME_NAME = "CYP";

      public override void GlobalContext()
      {
         base.GlobalContext();
         _moleculeExpressionTask = IoC.Resolve<IMoleculeExpressionTask<Individual>>();
         _ontogenyRepository = IoC.Resolve<IOntogenyRepository>();
         _expressionProfileUpdater = IoC.Resolve<IExpressionProfileUpdater>();
         _workspace = IoC.Resolve<ICoreWorkspace>();
      }
   }

   public class When_switching_ontogeny_from_none_to_CYP3A4_for_a_newborn : concern_for_OntogenySwitch
   {
      private double _ontogenyFactorBeforeSwitch;
      private double _ontogenyFactorAfterSwitch;

      public override void GlobalContext()
      {
         base.GlobalContext();

         // Create Individual (European ICRP male, Age 0 years)
         _individual = DomainFactoryForSpecs.CreateStandardIndividual(CoreConstants.Population.ICRP);
         _individual.OriginData.Age = new OriginDataParameter(0);
         _individual.AgeParameter.Value = 0;

         // Create enzyme CYP without ontogeny and add to individual
         _expressionProfile = DomainFactoryForSpecs.CreateExpressionProfile<IndividualEnzyme>(moleculeName: ENZYME_NAME);
         // Expression profile is created without ontogeny by default (NullOntogeny)
         _moleculeExpressionTask.AddExpressionProfile(_individual, _expressionProfile);

         // Create compound C1
         _compound = DomainFactoryForSpecs.CreateStandardCompound().WithName("C1");

         // Create Bolus IV administration protocol
         _protocol = DomainFactoryForSpecs.CreateStandardIVBolusProtocol();

         // Create simulation S1
         _simulation = DomainFactoryForSpecs.CreateSimulationWith(_individual, _compound, _protocol) as IndividualSimulation;

         // Setup project for building block update
         var project = new PKSimProject();
         project.AddBuildingBlock(_compound);
         project.AddBuildingBlock(_protocol);
         project.AddBuildingBlock(_simulation);
         project.AddBuildingBlock(_individual);
         project.AddBuildingBlock(_expressionProfile);
         _workspace.Project = project;

         // Get ontogeny factor before switch (should be 1)
         var ontogenyFactorParameter = _simulation.Model.Root.EntityAt<IParameter>(ENZYME_NAME, CoreConstants.Parameters.ONTOGENY_FACTOR);
         _ontogenyFactorBeforeSwitch = ontogenyFactorParameter.Value;

         // Change ontogeny in the enzyme to CYP3A4
         var cyp3a4Ontogeny = _ontogenyRepository.All().FindByName("CYP3A4");
         _expressionProfile.Molecule.Ontogeny = cyp3a4Ontogeny;
         
         // First sync the expression profile change to the individual
         _expressionProfileUpdater.SynchroniseSimulationSubjectWithExpressionProfile(_individual, _expressionProfile);

         // Update the individual in the simulation from the building block
         // This synchronizes expression profiles and updates parameters
         sut.UpdateParametersFromBuildingBlockInSimulation(_individual, _simulation);

         // Rebuild the simulation model to apply the ontogeny changes
         // Since ontogeny affects the model structure, we need to rebuild
         DomainFactoryForSpecs.AddModelToSimulation(_simulation);

         // Get ontogeny factor after switch (should be between 0.1 and 0.2 for a newborn)
         ontogenyFactorParameter = _simulation.Model.Root.EntityAt<IParameter>(ENZYME_NAME, CoreConstants.Parameters.ONTOGENY_FACTOR);
         _ontogenyFactorAfterSwitch = ontogenyFactorParameter.Value;
      }

      [Observation]
      public void should_have_ontogeny_factor_of_1_before_ontogeny_is_set()
      {
         _ontogenyFactorBeforeSwitch.ShouldBeEqualTo(1);
      }

      [Observation]
      public void should_have_ontogeny_factor_between_0_1_and_0_2_after_switching_to_CYP3A4()
      {
         _ontogenyFactorAfterSwitch.ShouldBeGreaterThan(0.1);
         _ontogenyFactorAfterSwitch.ShouldBeSmallerThan(0.2);
      }
   }
}
