﻿using System.Linq;
using System.Threading.Tasks;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Utility.Container;
using OSPSuite.Utility.Extensions;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using PKSim.Infrastructure;
using PKSim.Infrastructure.ProjectConverter;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Formulas;
using OSPSuite.Core.Domain.Services;
using SimulationRunOptions = PKSim.Core.Services.SimulationRunOptions;
using static OSPSuite.Core.Domain.Constants;

namespace PKSim.IntegrationTests
{
   public abstract class concern_for_ParentMetabolite : ContextForSimulationIntegration<Simulation>
   {
      protected Individual _individual;
      protected Protocol _protocol;
      protected EnzymaticProcess _parentMetabolizationCYP3A4;
      protected EnzymaticProcess _parentMetabolizationCYP2D6;
      protected Compound _compound;
      protected Compound _metabolite;
      protected IndividualMolecule _cyp3A4;
      private IndividualMolecule _cyp2D6;
      protected SimulationRunOptions _simulationRunOptions;

      public override void GlobalContext()
      {
         base.GlobalContext();
         _compound = DomainFactoryForSpecs.CreateStandardCompound().WithName("Parent");
         _metabolite = DomainFactoryForSpecs.CreateStandardCompound().WithName("Metabolite");
         _individual = DomainFactoryForSpecs.CreateStandardIndividual();
         _protocol = DomainFactoryForSpecs.CreateStandardIVBolusProtocol();

         _cyp3A4 = AddEnzymeTo(_individual, "CYP3A4");
         _parentMetabolizationCYP3A4 = AddEnzymaticProcess(_compound, _cyp3A4);

         _cyp2D6 = AddEnzymeTo(_individual, "CYP2D6");
         _parentMetabolizationCYP2D6 = AddEnzymaticProcess(_compound, _cyp2D6);

         _simulationRunOptions = new SimulationRunOptions {RaiseEvents = false};

      }

      protected EnzymaticProcess AddEnzymaticProcess(Compound compound, IndividualMolecule enzyme)
      {
         var compoundProcessRepository = IoC.Resolve<ICompoundProcessRepository>();
         var cloneManager = IoC.Resolve<ICloneManager>();
         var enzymaticProcess = cloneManager.Clone(compoundProcessRepository.ProcessByName(CoreConstantsForSpecs.Process.METABOLIZATION_SPECIFIC_FIRST_ORDER).DowncastTo<EnzymaticProcess>());
         enzymaticProcess.Name = "My Partial Process " + enzyme.Name;
         enzymaticProcess.MoleculeName = enzyme.Name;
         enzymaticProcess.Parameter(ConverterConstants.Parameters.CLspec).Value = 10;
         compound.AddProcess(enzymaticProcess);
         return enzymaticProcess;
      }

      protected IndividualMolecule AddEnzymeTo(Individual individual, string enzymeName)
      {
         var expressionProfileForEnzyme = DomainFactoryForSpecs.CreateExpressionProfileAndAddToIndividual<IndividualEnzyme>(individual, enzymeName, expressionProfile =>
         {
            var (enzyme, expressionProfileIndividual) = expressionProfile;

            var allExpressionParameters = expressionProfileIndividual.AllExpressionParametersFor(enzyme);
            allExpressionParameters[CoreConstants.Compartment.PERICENTRAL].Value = 1;
            allExpressionParameters[CoreConstants.Compartment.PERIPORTAL].Value = 1;

         });

         return expressionProfileForEnzyme.Molecule;
      }

      protected EnzymaticProcessSelection ProcessSelectionFor(EnzymaticProcess process)
      {
         var templateCompound = process.ParentCompound;
         return _simulation.CompoundPropertiesFor(templateCompound.Name)
            .Processes
            .MetabolizationSelection.AllPartialProcesses()
            .OfType<EnzymaticProcessSelection>()
            .Find(x => x.ProcessName == process.Name);
      }

      protected MoleculeAmount MetaboliteAmountInLiverPeriportalCellFor(EnzymaticProcess process, string metaboliteName = null)
      {
         return MetaboliteAmountInLiverZoneCellFor(process, CoreConstants.Compartment.PERIPORTAL, metaboliteName);
      }

      protected MoleculeAmount MetaboliteAmountInLiverPericentralCellFor(EnzymaticProcess process, string metaboliteName = null)
      {
         return MetaboliteAmountInLiverZoneCellFor(process, CoreConstants.Compartment.PERICENTRAL, metaboliteName);
      }

      protected MoleculeAmount MetaboliteAmountInLiverZoneCellFor(EnzymaticProcess process, string liverZone,  string metaboliteName)
      {
         var enzymaticProcessSelection = ProcessSelectionFor(process);

         var liverPeriportalCell = _simulation.Model.Root.EntityAt<Container>(Constants.ORGANISM, CoreConstants.Organ.LIVER,
            liverZone, CoreConstants.Compartment.INTRACELLULAR);

         var dynamicMetaboliteName = enzymaticProcessSelection.ProductName(CoreConstants.Molecule.Metabolite);
         return liverPeriportalCell.EntityAt<MoleculeAmount>(metaboliteName ?? dynamicMetaboliteName);
      }
   }

   public class When_creating_a_simulation_with_a_parent_and_metabolite_compound_linked_by_a_process : concern_for_ParentMetabolite
   {
      public override void GlobalContext()
      {
         base.GlobalContext();
         _simulation = DomainFactoryForSpecs.CreateModelLessSimulationWith(_individual, new[] {_compound, _metabolite}, new[] {_protocol, null}).DowncastTo<IndividualSimulation>();
         ProcessSelectionFor(_parentMetabolizationCYP3A4).MetaboliteName = _metabolite.Name;
         DomainFactoryForSpecs.AddModelToSimulation(_simulation);
      }

      [Observation]
      public void should_have_created_a_reaction_using_the_metabolite_as_product_in_liver_periportal_intracellular()
      {
         var reactionInLiver = _simulation.Model.Root.EntityAt<Reaction>(Constants.ORGANISM, CoreConstants.Organ.LIVER,
            CoreConstants.Compartment.PERIPORTAL, CoreConstants.Compartment.INTRACELLULAR,
            CompositeNameFor(_compound.Name, _parentMetabolizationCYP3A4.Name));

         reactionInLiver.ShouldNotBeNull();
         reactionInLiver.Products.Find(x => x.Partner.Name == _metabolite.Name).ShouldNotBeNull();
      }

      [Observation]
      public void should_have_created_a_reaction_using_the_metabolite_as_product_in_liver_pericentral_intracellular()
      {
         var reactionInLiver = _simulation.Model.Root.EntityAt<Reaction>(Constants.ORGANISM, CoreConstants.Organ.LIVER,
            CoreConstants.Compartment.PERICENTRAL, CoreConstants.Compartment.INTRACELLULAR,
            CompositeNameFor(_compound.Name, _parentMetabolizationCYP3A4.Name));

         reactionInLiver.ShouldNotBeNull();
         reactionInLiver.Products.Find(x => x.Partner.Name == _metabolite.Name).ShouldNotBeNull();
      }

      [Observation]
      public async Task should_be_able_to_run_the_simulation()
      {
         var simulationEngine = IoC.Resolve<IIndividualSimulationEngine>();
         await simulationEngine.RunAsync(_simulation, _simulationRunOptions);
         _simulation.HasResults.ShouldBeTrue();
      }

      [Observation]
      public void should_not_create_one_fraction_of_dose_observer_for_the_metabolite_defined_as_compound_in_the_simulation()
      {
         var cyp3A4MetaboliteInLiverCell = MetaboliteAmountInLiverPeriportalCellFor(_parentMetabolizationCYP3A4, _metabolite.Name);
         cyp3A4MetaboliteInLiverCell.Children.FindByName(CoreConstants.Observer.ObserverNameFrom(CoreConstants.Observer.FRACTION_OF_DOSE, _compound.Name)).ShouldBeNull();
      }

      [Observation]
      public void should_create_one_fraction_of_dose_observer_for_the_unspecified_metabolite_as_compound_in_the_simulation()
      {
         var cyp2D6MetaboliteInLiverCell = MetaboliteAmountInLiverPeriportalCellFor(_parentMetabolizationCYP2D6);
         cyp2D6MetaboliteInLiverCell.Children.FindByName(CoreConstants.Observer.ObserverNameFrom(CoreConstants.Observer.FRACTION_OF_DOSE, _compound.Name)).ShouldNotBeNull();
      }
   }

   public class When_creating_a_simulation_with_a_parent_m1_m2_process_chain : concern_for_ParentMetabolite
   {
      private EnzymaticProcess _metaboliteMetabolizationCYP3A4;

      public override void GlobalContext()
      {
         base.GlobalContext();
         _metaboliteMetabolizationCYP3A4 = AddEnzymaticProcess(_metabolite, _cyp3A4);
         _simulation = DomainFactoryForSpecs.CreateModelLessSimulationWith(_individual, new[] {_compound, _metabolite}, new[] {_protocol, null}).DowncastTo<IndividualSimulation>();

         //Compound => Metabolite1
         ProcessSelectionFor(_parentMetabolizationCYP3A4).MetaboliteName = _metabolite.Name;

         DomainFactoryForSpecs.AddModelToSimulation(_simulation);
      }

      [Observation]
      public void should_not_create_a_fraction_of_dose_observer_for_the_metabolite_defined_as_compound_in_the_simulation()
      {
         var cyp3A4MetaboliteInLiverCell = MetaboliteAmountInLiverPeriportalCellFor(_parentMetabolizationCYP3A4, _metabolite.Name);
         cyp3A4MetaboliteInLiverCell.Children.FindByName(CoreConstants.Observer.ObserverNameFrom(CoreConstants.Observer.FRACTION_OF_DOSE, _compound.Name)).ShouldBeNull();
      }

      [Observation]
      public void should_not_create_a_fraction_excreted_to_urine_observer_for_the_floating_metabolite_in_the_simulation()
      {
         var kidneyUrineMetabolite = _simulation.Model.Root.EntityAt<MoleculeAmount>(ORGANISM, CoreConstants.Organ.KIDNEY, CoreConstants.Compartment.URINE, _metabolite.Name);
         kidneyUrineMetabolite.Children.FindByName(CoreConstants.Observer.FRACTION_EXCRETED_TO_URINE).ShouldBeNull();
      }

      [Observation]
      public void should_create_a_fraction_of_dose_observer_for_the_unspecified_metabolite_in_the_simulation_liver_zone()
      {
         var cyp2D6MetaboliteInLiverCell = MetaboliteAmountInLiverPeriportalCellFor(_parentMetabolizationCYP2D6);
         cyp2D6MetaboliteInLiverCell.Children.FindByName(CoreConstants.Observer.ObserverNameFrom(CoreConstants.Observer.FRACTION_OF_DOSE, _compound.Name)).ShouldNotBeNull();
      }

      [Observation]
      public void should_create_a_fraction_of_dose_observer_for_the_unspecified_metabolite_in_the_liver_as_sum_of_the_fraction_in_liver_periportal_and_liver_pericentral()
      {
         var enzymaticProcessSelection = ProcessSelectionFor(_parentMetabolizationCYP2D6);
         var observerName = CoreConstants.Observer.ObserverNameFrom(CoreConstants.Observer.FRACTION_OF_DOSE, _compound.Name);
         var liverCellCYP2D6 = _simulation.Model.Root.EntityAt<Container>(Constants.ORGANISM, CoreConstants.Organ.LIVER, CoreConstants.Compartment.INTRACELLULAR, enzymaticProcessSelection.ProductName());
         var observerLiverCell = liverCellCYP2D6.Children.FindByName(CompositeNameFor(observerName, CoreConstants.Organ.LIVER, CoreConstants.Compartment.INTRACELLULAR)).DowncastTo<Observer>();
         observerLiverCell.ShouldNotBeNull();

         observerLiverCell.Formula.DowncastTo<ExplicitFormula>().FormulaString.ShouldBeEqualTo("(M_periportal + M_pericentral)/TotalDrugMass");
      }


      [Observation]
      public void should_not_create_a_fraction_of_dose_observer_for_the_metabolite_of_metabolite_in_the_simulation()
      {
         var cyp3A4MetaboliteMetaboliteInLiverCell = MetaboliteAmountInLiverPeriportalCellFor(_metaboliteMetabolizationCYP3A4);
         cyp3A4MetaboliteMetaboliteInLiverCell.Children.FindByName(CoreConstants.Observer.ObserverNameFrom(CoreConstants.Observer.FRACTION_OF_DOSE, _metabolite.Name)).ShouldBeNull();
      }
   }
}