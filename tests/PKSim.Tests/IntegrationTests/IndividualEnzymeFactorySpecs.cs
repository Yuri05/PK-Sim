using System.Collections.Generic;
using System.Linq;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Utility.Collections;
using OSPSuite.Utility.Container;
using OSPSuite.Utility.Extensions;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Services;
using PKSim.Infrastructure;
using static PKSim.Core.CoreConstants.Compartment;
using static PKSim.Core.CoreConstants.Organ;

namespace PKSim.IntegrationTests
{
   public abstract class concern_for_IndividualEnzymeFactory : ContextForIntegration<IIndividualEnzymeFactory>
   {
      protected Individual _individual;

      public override void GlobalContext()
      {
         base.GlobalContext();
         _individual = DomainFactoryForSpecs.CreateStandardIndividual();
         sut = IoC.Resolve<IIndividualEnzymeFactory>();
      }
   }

   public class When_creating_a_metabolism_expression_for_an_individual : concern_for_IndividualEnzymeFactory
   {
      private IndividualMolecule _molecule;
      private ICache<string, IParameter> _allExpressionParameters;

      public override void GlobalContext()
      {
         base.GlobalContext();
         _molecule = sut.AddMoleculeTo(_individual, "CYP3A4");
      }

      protected override void Because()
      {
         _allExpressionParameters = _individual.AllExpressionParametersFor(_molecule);
      }

      [Observation]
      public void should_return_a_metabolism_expression()
      {
         _molecule.ShouldBeAnInstanceOf<IndividualEnzyme>();
      }

      [Observation]
      public void should_return_an_expression_containing_at_least_the_container_blood_cells_and_plasma()
      {
         _allExpressionParameters.Contains(BLOOD_CELLS).ShouldBeTrue();
         _allExpressionParameters.Contains(PLASMA).ShouldBeTrue();
      }

      [Observation]
      public void should_have_marked_the_reference_concentration_parameter_as_variable()
      {
         _molecule.ReferenceConcentration.CanBeVaried.ShouldBeTrue();
         _molecule.ReferenceConcentration.CanBeVariedInPopulation.ShouldBeTrue();
      }

      [Observation]
      public void should_have_ensure_that_all_formula_have_different_ids()
      {
         var allInitialConcentrationParameters = _individual.AllMoleculeParametersFor(_molecule)
            .Where(x => x.IsNamed(CoreConstants.Parameters.INITIAL_CONCENTRATION))
            .ToList();

         var allFormulaIds = new HashSet<string>(allInitialConcentrationParameters.Select(x => x.Formula.Id));

         allInitialConcentrationParameters.Count.ShouldBeEqualTo(allFormulaIds.Count);
      }

      [Observation]
      public void should_have_set_all_initial_concentration_parameters_as_not_Variable()
      {
         var allInitialConcentrationParameters = _individual.AllMoleculeParametersFor(_molecule)
            .Where(x => x.IsNamed(CoreConstants.Parameters.INITIAL_CONCENTRATION))
            .ToList();


         allInitialConcentrationParameters.Each(x => x.CanBeVariedInPopulation.ShouldBeFalse());
      }

      [Observation]
      public void should_not_return_disease_state_parameters_as_part_of_global_expression_parameters()
      {
         _molecule.AllGlobalExpressionParameters.ShouldNotContain(_molecule.DiseaseFactorParameter);
      }
 
      [Observation]
      public void should_not_return_molecule_properties_parameters_as_part_of_global_expression_parameters()
      {
         _molecule.AllGlobalExpressionParameters.ShouldNotContain(_molecule.ReferenceConcentration,_molecule.HalfLifeIntestine,_molecule.HalfLifeLiver);
      }

      [Observation]
      public void should_not_return_ontogeny_parameters_as_part_of_global_expression_parameters()
      {
         _molecule.AllGlobalExpressionParameters.ShouldNotContain(_molecule.OntogenyFactorParameter, _molecule.OntogenyFactorGIParameter);
      }

   }

   public class When_creating_an_undefined_enzyme_for_a_given_individual : concern_for_IndividualEnzymeFactory
   {
      private IndividualEnzyme _undefined;

      public override void GlobalContext()
      {
         base.GlobalContext();

         //add the molecule in global context otherwise it will be added twice
         _undefined = sut.AddUndefinedLiverTo(_individual);
      }

      [Observation]
      public void should_add_the_relative_expression_to_periportal_and_pericentral_and_set_the_value_to_100()
      {
         var allExpressionsParameters = _individual.AllExpressionParametersFor(_undefined);
         allExpressionsParameters[PERICENTRAL].Value.ShouldBeEqualTo(1);
         allExpressionsParameters[PERIPORTAL].Value.ShouldBeEqualTo(1);
      }

      [Observation]
      public void should_have_enhanced_the_path_of_the_initial_concentration_to_be_start_with_root()
      {
         var liverIntracellularUndefined = _individual.EntityAt<IParameter>(Constants.ORGANISM, LIVER, PERIPORTAL, INTRACELLULAR, CoreConstants.Molecule.UndefinedLiver, "Initial concentration");
         liverIntracellularUndefined.ShouldNotBeNull();

         var objectPath = liverIntracellularUndefined.Formula.ObjectPaths.First(x => x.Alias == "RC");
         objectPath[0].ShouldBeEqualTo(Constants.ROOT);
      }
   }
}