using System.Collections.Generic;
using FakeItEasy;
using OSPSuite.BDDHelper;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.Services.ParameterIdentifications;
using OSPSuite.Core.Import;
using OSPSuite.Core.Serialization.Xml;
using OSPSuite.Core.Services;
using OSPSuite.Infrastructure.Import.Services;
using OSPSuite.Utility.Events;
using PKSim.Core;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using PKSim.Infrastructure.Services;
using IObservedDataTask = PKSim.Core.Services.IObservedDataTask;
using IContainer = OSPSuite.Utility.Container.IContainer;

namespace PKSim.Infrastructure
{
   public abstract class concern_for_ImportObservedDataTask : ContextSpecification<IImportObservedDataTask>
   {
      protected IParameterIdentificationTask _parameterIdentificationTask;

      protected override void Context()
      {
         var dataImporter = A.Fake<IDataImporter>();
         var executionContext = A.Fake<IExecutionContext>();
         var buildingBlockRepository = A.Fake<IBuildingBlockRepository>();
         var speciesRepository = A.Fake<ISpeciesRepository>();
         var defaultIndividualRetriever = A.Fake<IDefaultIndividualRetriever>();
         var representationInfoRepository = A.Fake<IRepresentationInfoRepository>();
         var observedDataTask = A.Fake<IObservedDataTask>();
         var parameterChangeUpdater = A.Fake<IParameterChangeUpdater>();
         var dialogCreator = A.Fake<IDialogCreator>();
         var container = A.Fake<IContainer>();
         var modelingXmlSerializerRepository = A.Fake<IOSPSuiteXmlSerializerRepository>();
         var eventPublisher = A.Fake<IEventPublisher>();
         _parameterIdentificationTask = A.Fake<IParameterIdentificationTask>();

         var overwrittenData = new List<DataRepository>
         {
            A.Fake<DataRepository>(),
         };

         A.CallTo(() => dataImporter.CalculateReloadDataSetsFromConfiguration(A<IReadOnlyList<DataRepository>>.Ignored, A<IReadOnlyList<DataRepository>>.Ignored))
            .Returns(new ReloadDataSets(new List<DataRepository>(), overwrittenData, new List<DataRepository>()));

         sut = new ImportObservedDataTask(dataImporter, executionContext, buildingBlockRepository, speciesRepository,
            defaultIndividualRetriever, representationInfoRepository, observedDataTask, parameterChangeUpdater,
            dialogCreator, container, modelingXmlSerializerRepository, eventPublisher, _parameterIdentificationTask);
      }
   }

   public class When_adding_and_replacing_observed_data_from_configuration_to_project : concern_for_ImportObservedDataTask
   {
      protected override void Because()
      {
         sut.AddAndReplaceObservedDataFromConfigurationToProject(A.Fake<ImporterConfiguration>(), A.Fake<IReadOnlyList<DataRepository>>());
      }

      [Observation]
      public void should_call_update_parameter_identification_Using()
      {
         A.CallTo(() => _parameterIdentificationTask.UpdateParameterIdentificationsUsing(A<IReadOnlyList<DataRepository>>._))
            .MustHaveHappened();
      }
   }
}