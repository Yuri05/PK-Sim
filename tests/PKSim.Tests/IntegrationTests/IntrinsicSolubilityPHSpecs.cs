using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Formulas;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Infrastructure;
using Compound = PKSim.Core.Model.Compound;
using static PKSim.Core.CoreConstants.Groups;
using static PKSim.CoreConstantsForSpecs;

namespace PKSim.IntegrationTests
{
   //https://github.com/Open-Systems-Pharmacology/PK-Sim/issues/3730
   public abstract class concern_for_pH_intrinsic_solubility : ContextForIntegration<ICompoundFactory>
   {
      protected Compound _compound;

      protected override void Context()
      {
         base.Context();
         _compound = DomainFactoryForSpecs.CreateStandardCompound();
      }

      protected IParameter PHIntrinsicSolubilityParameter => _compound.EntityAt<IParameter>(Parameters.PH_INTRINSIC_SOLUBILITY);

      protected void SetCompoundType(int index, CompoundType compoundType, double pKa)
      {
         _compound.Parameter(Constants.Parameters.ParameterCompoundType(index)).Value = (int) compoundType;
         _compound.Parameter(CoreConstants.Parameters.ParameterPKa(index)).Value = pKa;
      }
   }

   public class When_creating_a_compound_with_the_intrinsic_solubility_parameter : concern_for_pH_intrinsic_solubility
   {
      [Observation]
      public void should_add_the_ph_intrinsic_solubility_parameter()
      {
         PHIntrinsicSolubilityParameter.ShouldNotBeNull();
      }

      [Observation]
      public void should_define_the_parameter_as_visible_and_read_only_in_the_advanced_intestinal_solubility_group()
      {
         var parameter = PHIntrinsicSolubilityParameter;
         parameter.Visible.ShouldBeTrue();
         parameter.Editable.ShouldBeFalse();
         parameter.GroupName.ShouldBeEqualTo(COMPOUND_ADVANCED_SOLUBILITY);
      }

      [Observation]
      public void should_calculate_the_parameter_value_with_a_formula()
      {
         PHIntrinsicSolubilityParameter.Formula.IsConstant().ShouldBeFalse();
      }
   }

   public class When_calculating_the_ph_intrinsic_solubility_for_a_neutral_compound : concern_for_pH_intrinsic_solubility
   {
      [Observation]
      public void should_return_zero()
      {
         PHIntrinsicSolubilityParameter.Value.ShouldBeEqualTo(0, 1e-10);
      }
   }

   public class When_calculating_the_ph_intrinsic_solubility_for_a_compound_with_bases_only : concern_for_pH_intrinsic_solubility
   {
      protected override void Because()
      {
         SetCompoundType(0, CompoundType.Base, 9);
      }

      [Observation]
      public void should_return_fourteen()
      {
         PHIntrinsicSolubilityParameter.Value.ShouldBeEqualTo(14, 1e-10);
      }
   }

   public class When_calculating_the_ph_intrinsic_solubility_for_a_compound_with_acids_only : concern_for_pH_intrinsic_solubility
   {
      protected override void Because()
      {
         SetCompoundType(0, CompoundType.Acid, 4);
         SetCompoundType(1, CompoundType.Acid, 6);
      }

      [Observation]
      public void should_return_zero()
      {
         PHIntrinsicSolubilityParameter.Value.ShouldBeEqualTo(0, 1e-10);
      }
   }

   public class When_calculating_the_ph_intrinsic_solubility_for_a_zwitterionic_compound_with_one_base : concern_for_pH_intrinsic_solubility
   {
      protected override void Because()
      {
         SetCompoundType(0, CompoundType.Acid, 4);
         SetCompoundType(1, CompoundType.Base, 9);
      }

      [Observation]
      public void should_return_the_mean_of_the_smallest_acid_pKa_and_the_base_pKa()
      {
         PHIntrinsicSolubilityParameter.Value.ShouldBeEqualTo((4 + 9) / 2.0, 1e-10);
      }
   }

   public class When_creating_a_simulation_with_a_compound : ContextForIntegration<ISimulationConstructor>
   {
      private Simulation _simulation;
      private Compound _compound;

      protected override void Context()
      {
         base.Context();
         var individual = DomainFactoryForSpecs.CreateStandardIndividual();
         _compound = DomainFactoryForSpecs.CreateStandardCompound();
         var protocol = DomainFactoryForSpecs.CreateStandardIVBolusProtocol();
         _simulation = DomainFactoryForSpecs.CreateSimulationWith(individual, _compound, protocol);
      }

      [Observation]
      public void should_link_the_parameter_to_the_corresponding_parameter_of_the_compound_building_block()
      {
         var parameter = _simulation.Model.Root.EntityAt<IParameter>(_compound.Name, Parameters.PH_INTRINSIC_SOLUBILITY);
         var compoundInSimulation = _simulation.Compounds[0];
         var parameterInCompound = compoundInSimulation.EntityAt<IParameter>(Parameters.PH_INTRINSIC_SOLUBILITY);
         parameter.Origin.ParameterId.ShouldBeEqualTo(parameterInCompound.Id);
         parameter.Origin.BuilingBlockId.ShouldBeEqualTo(compoundInSimulation.Id);
      }

      [Observation]
      public void should_create_the_ph_intrinsic_solubility_parameter()
      {
         var parameter = _simulation.Model.Root.EntityAt<IParameter>(_compound.Name, Parameters.PH_INTRINSIC_SOLUBILITY);
         parameter.ShouldNotBeNull();
         parameter.Value.ShouldBeEqualTo(0, 1e-10);
      }
   }

   public class When_calculating_the_ph_intrinsic_solubility_for_a_zwitterionic_compound_with_two_bases : concern_for_pH_intrinsic_solubility
   {
      protected override void Because()
      {
         SetCompoundType(0, CompoundType.Acid, 4);
         SetCompoundType(1, CompoundType.Base, 7);
         SetCompoundType(2, CompoundType.Base, 10);
      }

      [Observation]
      public void should_return_the_mean_of_the_largest_of_acid_and_smallest_base_pKa_and_the_largest_base_pKa()
      {
         PHIntrinsicSolubilityParameter.Value.ShouldBeEqualTo((7 + 10) / 2.0, 1e-10);
      }
   }
}
