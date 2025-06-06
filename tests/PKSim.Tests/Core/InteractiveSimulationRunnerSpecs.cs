﻿using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Events;
using PKSim.Core.Chart;
using PKSim.Core.Model;
using PKSim.Core.Services;
using SimulationRunOptions = PKSim.Core.Services.SimulationRunOptions;

namespace PKSim.Core
{
   public abstract class concern_for_InteractiveSimulationRunner : ContextSpecificationAsync<IInteractiveSimulationRunner>
   {
      protected ISimulationAnalysisCreator _simulationAnalysisCreator;
      private ICloner _cloner;
      protected ISimulationSettingsRetriever _simulationSettingsRetriever;
      protected ISimulationRunner _simulationRunner;
      protected ILazyLoadTask _lazyLoadTask;
      protected IExecutionContext _executionContext;

      protected override Task Context()
      {
         _simulationAnalysisCreator = A.Fake<ISimulationAnalysisCreator>();
         _cloner = A.Fake<ICloner>();
         _simulationSettingsRetriever = A.Fake<ISimulationSettingsRetriever>();
         _simulationRunner = A.Fake<ISimulationRunner>();
         _lazyLoadTask = A.Fake<ILazyLoadTask>();
         _executionContext = A.Fake<IExecutionContext>();
         sut = new InteractiveSimulationRunner(_simulationSettingsRetriever, _simulationRunner, _cloner, _simulationAnalysisCreator, _lazyLoadTask, _executionContext);
         return _completed;
      }
   }

   public class When_an_output_with_mapping_is_removed_from_simulation_run : concern_for_InteractiveSimulationRunner
   {
      private IndividualSimulation _simulation;
      private QuantitySelection _quantitySelection;
      private OutputSelections _modifiedOutputSelections;

      protected override async Task Context()
      {
         await base.Context();
         _simulation = A.Fake<IndividualSimulation>();
         _simulation.OutputSelections = new OutputSelections();
         _quantitySelection = new QuantitySelection("output|selection");
         _simulation.OutputSelections.AddOutput(_quantitySelection);
         _modifiedOutputSelections = new OutputSelections();
         _simulation.OutputMappings.Add(new OutputMapping { OutputSelection = new SimulationQuantitySelection(_simulation, _quantitySelection) });

         A.CallTo(() => _simulationSettingsRetriever.SettingsFor(_simulation)).Returns(_modifiedOutputSelections);
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_simulation, true);
      }

      [Observation]
      public void the_runner_should_remove_the_mapping_from_the_simulation()
      {
         _simulation.OutputMappings.ShouldBeEmpty();
      }
   }

   public class When_the_simulation_is_notified_that_a_simulation_with_results_but_not_plot_was_calculated : concern_for_InteractiveSimulationRunner
   {
      private IndividualSimulation _simulation;

      protected override async Task Context()
      {
         await base.Context();
         _simulation = A.Fake<IndividualSimulation>();
         A.CallTo(() => _simulation.Analyses).Returns(new List<ISimulationAnalysis>());
         A.CallTo(() => _simulation.HasResults).Returns(true);
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_simulation, false);
      }

      [Observation]
      public void should_load_the_simulation()
      {
         A.CallTo(() => _lazyLoadTask.Load((Simulation)_simulation)).MustHaveHappened();
      }

      [Observation]
      public void should_create_a_chart_for_the_simulation()
      {
         A.CallTo(() => _simulationAnalysisCreator.CreateAnalysisFor(_simulation)).MustHaveHappened();
      }
   }

   public class When_the_simulation_is_notified_that_a_simulation_without_results_and_plot_was_calculated : concern_for_InteractiveSimulationRunner
   {
      private IndividualSimulation _simulation;

      protected override async Task Context()
      {
         await base.Context();
         _simulation = A.Fake<IndividualSimulation>();
         A.CallTo(() => _simulation.Analyses).Returns(new List<SimulationTimeProfileChart>());
         A.CallTo(() => _simulation.HasResults).Returns(false);
         await sut.RunSimulation(_simulation, false);
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_simulation, false);
         ;
      }

      [Observation]
      public void should_not_create_chart_for_the_simulation()
      {
         A.CallTo(() => _simulationAnalysisCreator.CreateAnalysisFor(_simulation)).MustNotHaveHappened();
      }
   }

   public class When_the_simulation_is_notified_that_a_simulation_with_results_and_plot_was_calculated : concern_for_InteractiveSimulationRunner
   {
      private IndividualSimulation _simulation;

      protected override async Task Context()
      {
         await base.Context();
         _simulation = A.Fake<IndividualSimulation>();
         A.CallTo(() => _simulation.Analyses).Returns(new List<ISimulationAnalysis> { A.Fake<ISimulationAnalysis>() });
         A.CallTo(() => _simulation.HasResults).Returns(true);
         await sut.RunSimulation(_simulation, false);
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_simulation, false);
      }

      [Observation]
      public void should_not_create_chart_for_the_simulation()
      {
         A.CallTo(() => _simulationAnalysisCreator.CreateAnalysisFor(_simulation)).MustNotHaveHappened();
      }
   }

   public class When_the_simulation_runner_is_starting_a_simulation_run_for_a_given_population_simulation : concern_for_InteractiveSimulationRunner
   {
      private PopulationSimulation _populationSimulation;

      protected override async Task Context()
      {
         await base.Context();
         _populationSimulation = A.Fake<PopulationSimulation>();
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_populationSimulation, true);
      }

      [Observation]
      public void should_ask_the_user_to_select_the_quantities_that_should_be_persisted_for_the_run()
      {
         A.CallTo(() => _simulationSettingsRetriever.SettingsFor(_populationSimulation)).MustHaveHappened();
      }

      [Observation]
      public void should_raise_the_output_selection_changed_event()
      {
         A.CallTo(() => _executionContext.PublishEvent(A<SimulationOutputSelectionsChangedEvent>._)).MustHaveHappened();
      }
   }

   public class When_the_user_cancels_the_population_simulation_run : concern_for_InteractiveSimulationRunner
   {
      private PopulationSimulation _populationSimulation;

      protected override async Task Context()
      {
         await base.Context();
         _populationSimulation = A.Fake<PopulationSimulation>();

         A.CallTo(() => _simulationSettingsRetriever.SettingsFor(_populationSimulation)).Returns(null);
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_populationSimulation, true);
      }

      [Observation]
      public void should_not_run_the_population_simulation()
      {
         A.CallTo(() => _simulationRunner.RunSimulation(_populationSimulation, A<SimulationRunOptions>._)).MustNotHaveHappened();
      }

      [Observation]
      public void should_not_raise_the_output_selection_changed_event()
      {
         A.CallTo(() => _executionContext.PublishEvent(A<SimulationOutputSelectionsChangedEvent>._)).MustNotHaveHappened();
      }

      [Observation]
      public void should_not_update_the_population_settings_in_the_population()
      {
         _populationSimulation.OutputSelections.ShouldNotBeNull();
      }
   }

   public class When_the_user_confirms_the_simulation_run : concern_for_InteractiveSimulationRunner
   {
      private PopulationSimulation _populationSimulation;
      private OutputSelections _newPopulationSettings;

      protected override async Task Context()
      {
         await base.Context();
         _newPopulationSettings = new OutputSelections();
         _populationSimulation = A.Fake<PopulationSimulation>();
         A.CallTo(() => _simulationSettingsRetriever.SettingsFor(_populationSimulation)).Returns(_newPopulationSettings);
      }

      protected override Task Because()
      {
         return sut.RunSimulation(_populationSimulation, true);
      }

      [Observation]
      public void should_run_the_population_simulation()
      {
         A.CallTo(() => _simulationRunner.RunSimulation(_populationSimulation, A<SimulationRunOptions>._)).MustHaveHappened();
      }

      [Observation]
      public void should_update_the_population_settings_in_the_popoulation()
      {
         _populationSimulation.OutputSelections.AllOutputs.ShouldOnlyContain(_newPopulationSettings.AllOutputs);
      }
   }
}