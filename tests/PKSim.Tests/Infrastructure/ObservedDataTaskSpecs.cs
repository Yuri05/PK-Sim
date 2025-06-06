using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using OSPSuite.Assets;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Commands;
using OSPSuite.Core.Commands.Core;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.ParameterIdentifications;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.Core.Serialization.Xml;
using OSPSuite.Core.Services;
using OSPSuite.Presentation.Core;
using OSPSuite.Utility.Exceptions;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Services;
using PKSim.Presentation.Presenters.Snapshots;
using IObservedDataTask = PKSim.Core.Services.IObservedDataTask;
using ObservedDataTask = PKSim.Infrastructure.Services.ObservedDataTask;

namespace PKSim.Infrastructure
{
   public abstract class concern_for_ObservedDataTask : ContextSpecificationAsync<IObservedDataTask>
   {
      protected IExecutionContext _executionContext;
      protected IDialogCreator _dialogCreator;
      protected IApplicationController _applicationController;
      private IDataRepositoryExportTask _dataRepositoryTask;
      protected IContainerTask _containerTask;
      protected ITemplateTask _templateTask;
      protected PKSimProject _project;
      private IPKSimProjectRetriever _projectRetriever;
      private IObjectTypeResolver _objectTypeResolver;
      private IParameterChangeUpdater _parameterChangeUpdater;
      protected IPKMLPersistor _pkmlPersistor;
      protected IOutputMappingMatchingTask _outputMappingMatchingTask;
      protected IConfirmationManager _confirmationManager;

      protected override Task Context()
      {
         _containerTask = A.Fake<IContainerTask>();
         _projectRetriever = A.Fake<IPKSimProjectRetriever>();
         _dataRepositoryTask = A.Fake<IDataRepositoryExportTask>();
         _executionContext = A.Fake<IExecutionContext>();
         _dialogCreator = A.Fake<IDialogCreator>();
         _applicationController = A.Fake<IApplicationController>();
         _templateTask = A.Fake<ITemplateTask>();
         _parameterChangeUpdater = A.Fake<IParameterChangeUpdater>();
         _pkmlPersistor= A.Fake<IPKMLPersistor>();
         _outputMappingMatchingTask = A.Fake<IOutputMappingMatchingTask>();
         _confirmationManager = A.Fake<IConfirmationManager>();
         _project = new PKSimProject();
         A.CallTo(() => _projectRetriever.CurrentProject).Returns(_project);
         A.CallTo(() => _projectRetriever.Current).Returns(_project);
         A.CallTo(() => _executionContext.Project).Returns(_project);
         A.CallTo(() => _confirmationManager.IsConfirmationSuppressed(A<ConfirmationFlags>._)).Returns(false);

         _objectTypeResolver = A.Fake<IObjectTypeResolver>();
         sut = new ObservedDataTask(_projectRetriever, _executionContext, _dialogCreator, _applicationController,
            _dataRepositoryTask, _templateTask, _containerTask, _parameterChangeUpdater, _pkmlPersistor, _objectTypeResolver, _outputMappingMatchingTask, _confirmationManager);

         return _completed;
      }
   }

   public class When_loading_observed_data_from_snapshot : concern_for_ObservedDataTask
   {
      private DataRepository _mappedObservedData;

      protected override async Task Context()
      {
         await base.Context();
         _mappedObservedData = new DataRepository();
         var loadFromSnapshotPresenter= A.Fake<ILoadFromSnapshotPresenter<DataRepository>>();
         A.CallTo(() => _applicationController.Start<ILoadFromSnapshotPresenter<DataRepository>>()).Returns(loadFromSnapshotPresenter);
         A.CallTo(() => loadFromSnapshotPresenter.LoadModelFromSnapshot()).Returns(new[] {_mappedObservedData});
      }

      protected override Task Because()
      {
         sut.LoadFromSnapshot();
         return _completed;
      }

      [Observation]
      public void the_mapped_data_repository_should_be_added_to_the_project()
      {
         _project.AllObservedData.ShouldContain(_mappedObservedData);
      }
   }

   public class When_removing_some_observed_data_from_the_project : concern_for_ObservedDataTask
   {
      private DataRepository _dataRepository;
      private Simulation _sim1;
      private Simulation _sim2;

      protected override async Task Context()
      {
         await base.Context();

         _dataRepository = DomainHelperForSpecs.ObservedData();
         _sim1 = new IndividualSimulation {Name = "Sim1"};
         _sim2 = new IndividualSimulation {Name = "Sim2"};
         _sim1.AddUsedObservedData(_dataRepository);
         _project.AddBuildingBlock(_sim1);
         _project.AddBuildingBlock(_sim2);
      }

      [Observation]
      public void should_throw_an_exception_if_the_observed_data_are_still_being_used_in_a_simulation()
      {
         The.Action(() => sut.Delete(_dataRepository)).ShouldThrowAn<CannotDeleteObservedDataException>();
      }
   }

   public class when_adding_some_observed_data_to_a_chart_belonging_to_a_given_simulation : concern_for_ObservedDataTask
   {
      private Simulation _sim;
      private DataRepository _observedData;
      private IDimension _dimension;

      protected override async Task Context()
      {
         await base.Context();

         _dimension = A.Fake<IDimension>();
         _sim = new IndividualSimulation();
         _observedData = new DataRepository("toto");

         var baseGrid = new BaseGrid("dimension", _dimension)
         {
            Values = new List<float>() {0.1f, 0.3f}
         };
         var values = new DataColumn("column", _dimension, baseGrid);
         _observedData.Add(values);
      }

      protected override Task Because()
      {
         sut.AddObservedDataToAnalysable(new[] {_observedData}, _sim);
         return _completed;
      }

      [Observation]
      public void should_also_add_the_observed_data_in_the_simulation()
      {
         _sim.UsesObservedData(_observedData).ShouldBeTrue();
      }
   }

   public class When_adding_an_observed_data_from_template : concern_for_ObservedDataTask
   {
      private DataRepository _newObservedData;
      private ICommand _command;

      protected override async Task Context()
      {
         await base.Context();

         _newObservedData = new DataRepository("toto");
         A.CallTo(_templateTask).WithReturnType<Task<IReadOnlyList<DataRepository>>>().Returns(new []{_newObservedData });
         A.CallTo(() => _containerTask.CreateUniqueName(_project.AllObservedData, _newObservedData.Name, true)).Returns("TOTO");
         A.CallTo(() => _executionContext.AddToHistory(A<ICommand>._))
            .Invokes(x => _command = x.GetArgument<ICommand>(0));
      }

      protected override Task Because()
      {
         return sut.LoadObservedDataFromTemplateAsync();
      }

      [Observation]
      public void should_leverage_the_template_task_to_load_a_template_from_the_database()
      {
         A.CallTo(() => _templateTask.LoadFromTemplateAsync<DataRepository>(TemplateType.ObservedData)).MustHaveHappened();
      }

      [Observation]
      public void should_add_the_template_to_the_project_assuring_a_unique_name_if_one_was_selected()
      {
         _newObservedData.Name.ShouldBeEqualTo("TOTO");
         _project.AllObservedData.ShouldContain(_newObservedData);
      }

      [Observation]
      public void should_add_a_command_to_the_history()
      {
         _command.ShouldBeAnInstanceOf<AddObservedDataToProjectCommand>();
      }
   }

   public class When_removing_all_observed_data_and_one_is_used_by_parameter_identification : concern_for_ObservedDataTask
   {
      private DataRepository _usedDataRepository;
      private Simulation _observedDataUser;
      private DataRepository _unUsedDataRepository;

      protected override async Task Context()
      {
         await base.Context();

         _usedDataRepository = new DataRepository("id");
         _unUsedDataRepository = new DataRepository("anotherid");
         _observedDataUser = A.Fake<Simulation>();
         A.CallTo(() => _observedDataUser.UsesObservedData(_usedDataRepository)).Returns(true);
         A.CallTo(() => _observedDataUser.UsesObservedData(_unUsedDataRepository)).Returns(false);
         A.CallTo(() => _dialogCreator.MessageBoxYesNo(A<string>._, ViewResult.Yes)).Returns(ViewResult.OK);
         _project.AddBuildingBlock(_observedDataUser);
         _project.AddObservedData(_usedDataRepository);
         _project.AddObservedData(_unUsedDataRepository);
      }

      protected override Task Because()
      {
         sut.DeleteAll();
         return _completed;
      }

      [Observation]
      public void the_unused_data_should_be_deleted()
      {
         _project.AllObservedData.ShouldContain(_usedDataRepository);
      }

      [Observation]
      public void the_used_data_should_not_be_deleted()
      {
         _project.AllObservedData.ShouldNotContain(_unUsedDataRepository);
      }
   }

   public class When_removing_observed_data_used_by_parameter_identification : concern_for_ObservedDataTask
   {
      private DataRepository _dataRepository;
      private Simulation _observedDataUser;

      protected override async Task Context()
      {
         await base.Context();


         _dataRepository = new DataRepository("id");
         _observedDataUser = A.Fake<Simulation>();
         A.CallTo(() => _observedDataUser.UsesObservedData(_dataRepository)).Returns(true);
         A.CallTo(() => _dialogCreator.MessageBoxYesNo(A<string>._, ViewResult.Yes)).Returns(ViewResult.OK);
         _project.AddBuildingBlock(_observedDataUser);
      }

      [Observation]
      public void the_observed_data_should_not_be_removed_from_the_project()
      {
         The.Action(() => sut.Delete(new List<DataRepository> {_dataRepository})).ShouldThrowAn<OSPSuiteException>();
      }
   }

   public class When_removing_observed_data_that_are_used_in_a_parameter_identification_for_the_given_simulation : concern_for_ObservedDataTask
   {
      private UsedObservedData _usedObservedData;
      private Simulation _simulation;
      private DataRepository _dataRepository;
      private ParameterIdentification _parameterIdentification;

      protected override async Task Context()
      {
         await base.Context();

         _simulation = new IndividualSimulation();
         _usedObservedData = new UsedObservedData {Id = "dataRepositoryId", Simulation = _simulation};
         _dataRepository = new DataRepository(_usedObservedData.Id);
         _simulation.AddUsedObservedData(_usedObservedData);
         _parameterIdentification = new ParameterIdentification();
         _project.AddObservedData(_dataRepository);
         _project.AddParameterIdentification(_parameterIdentification);

         var outputMapping = A.Fake<OutputMapping>();
         A.CallTo(() => outputMapping.UsesObservedData(_dataRepository)).Returns(true);
         A.CallTo(() => outputMapping.UsesSimulation(_simulation)).Returns(true);
         _parameterIdentification.AddOutputMapping(outputMapping);
      }

      protected override Task Because()
      {
         sut.RemoveUsedObservedDataFromSimulation(new[] {_usedObservedData});
         return _completed;
      }

      [Observation]
      public void should_not_remove_observed_data_from_simulation()
      {
         _simulation.UsedObservedData.ShouldContain(_usedObservedData);
      }
   }

   public class When_removing_observed_data_that_are_used_in_a_parameter_identification_for_another_simulation : concern_for_ObservedDataTask
   {
      private UsedObservedData _usedObservedData;
      private Simulation _simulation;
      private DataRepository _dataRepository;
      private ParameterIdentification _parameterIdentification;

      protected override async Task Context()
      {
         await base.Context();

         _simulation = new IndividualSimulation();
         _usedObservedData = new UsedObservedData {Id = "dataRepositoryId", Simulation = _simulation};
         _dataRepository = new DataRepository(_usedObservedData.Id);
         _simulation.AddUsedObservedData(_usedObservedData);
         _parameterIdentification = new ParameterIdentification();
         _project.AddObservedData(_dataRepository);
         _project.AddParameterIdentification(_parameterIdentification);

         var outputMapping = A.Fake<OutputMapping>();
         A.CallTo(() => outputMapping.UsesObservedData(_dataRepository)).Returns(true);
         A.CallTo(() => outputMapping.UsesSimulation(_simulation)).Returns(false);
         A.CallTo(() => _confirmationManager.IsConfirmationSuppressed(ConfirmationFlags.ObservedDataEntryRemoved)).Returns(true);
         _parameterIdentification.AddOutputMapping(outputMapping);
      }

      protected override Task Because()
      {
         sut.RemoveUsedObservedDataFromSimulation(new[] {_usedObservedData});
         return _completed;
      }

      [Observation]
      public void should_remove_observed_data_from_simulation()
      {
         _simulation.UsedObservedData.ShouldNotContain(_usedObservedData);
      }
   }

   public class When_removing_some_used_observed_data_from_simulations : concern_for_ObservedDataTask
   {
      private UsedObservedData _usedObservedData1;
      private UsedObservedData _usedObservedData2;
      private UsedObservedData _usedObservedData3;
      private DataRepository _repo1;
      private DataRepository _repo2;
      private Simulation _sim1;
      private Simulation _sim2;

      protected override async Task Context()
      {
         await base.Context();

         _sim1 = new IndividualSimulation();
         _sim2 = new IndividualSimulation();
         _repo1 = new DataRepository("1");
         _repo2 = new DataRepository("2");
         _usedObservedData1 = new UsedObservedData {Id = _repo1.Id};
         _usedObservedData2 = new UsedObservedData {Id = _repo2.Id};
         _usedObservedData3 = new UsedObservedData {Id = _repo1.Id};

         _sim1.AddUsedObservedData(_usedObservedData1);
         _sim1.AddUsedObservedData(_usedObservedData2);
         _sim2.AddUsedObservedData(_usedObservedData3);

         A.CallTo(_dialogCreator).WithReturnType<ViewResult>().Returns(ViewResult.Yes);

         _project.AddObservedData(_repo1);
         _project.AddObservedData(_repo2);
      }

      protected override Task Because()
      {
         sut.RemoveUsedObservedDataFromSimulation(new[] {_usedObservedData1, _usedObservedData2, _usedObservedData3});
         return _completed;
      }

      [Observation]
      public void should_ask_the_user_to_confirm_the_removal()
      {
         A.CallTo(() => _dialogCreator.MessageBoxConfirm(Captions.ReallyRemoveObservedDataFromSimulation, A<Action>._, ViewResult.Yes)).MustHaveHappened();
      }

      [Observation]
      public void should_load_all_simulations()
      {
         //use the overload defined in core
         A.CallTo(() => _executionContext.Load((IObjectBase)_sim1)).MustHaveHappened();
         A.CallTo(() => _executionContext.Load((IObjectBase)_sim2)).MustHaveHappened();
      }

      [Observation]
      public void should_remove_the_used_observed_data_from_the_simulation()
      {
         _sim1.UsesObservedData(_repo1).ShouldBeFalse();
         _sim1.UsesObservedData(_repo2).ShouldBeFalse();
         _sim2.UsesObservedData(_repo1).ShouldBeFalse();
         _sim2.UsesObservedData(_repo2).ShouldBeFalse();
      }
   }

   public class When_exporting_observed_data_to_pkml_and_user_cancels_action : concern_for_ObservedDataTask
   {
      private DataRepository _observedData;

      protected override async Task Context()
      {
         await base.Context();

         _observedData = new DataRepository();
         A.CallTo(_dialogCreator).WithReturnType<string>().Returns(string.Empty);
      }

      protected override Task Because()
      {
         sut.ExportToPkml(_observedData);
         return _completed;
      }

      [Observation]
      public void should_not_export_the_data()
      {
         A.CallTo(() => _pkmlPersistor.SaveToPKML(_observedData, A<string>._)).MustNotHaveHappened();
      }
   }

   public class When_exporting_observed_data_to_pkml_and_user_selects_a_file : concern_for_ObservedDataTask
   {
      private DataRepository _observedData;
      private string _fileName;

      protected override async Task Context()
      {
         await base.Context();

         _observedData = new DataRepository();
         _fileName = "XX";
         A.CallTo(_dialogCreator).WithReturnType<string>().Returns(_fileName);
      }

      protected override Task Because()
      {
         sut.ExportToPkml(_observedData);
         return  _completed;
      }

      [Observation]
      public void should_export_the_data()
      {
         A.CallTo(() => _pkmlPersistor.SaveToPKML( _observedData, _fileName)).MustHaveHappened();
      }
   }

   public class DataRepositoryEqualityComparer : GenericEqualityComparer<DataRepository>
   {  

   }
}