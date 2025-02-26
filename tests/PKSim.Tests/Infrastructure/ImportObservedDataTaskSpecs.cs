using System;
using System.Collections;
using FakeItEasy;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.Mappers;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Domain.Services.ParameterIdentifications;
using OSPSuite.Core.Journal;
using OSPSuite.Core.Serialization.Exchange;
using OSPSuite.Core.Serialization.Xml;
using OSPSuite.Core.Services;
using OSPSuite.Infrastructure.Import.Services;
using OSPSuite.Utility;
using OSPSuite.Utility.Events;
using OSPSuite.Utility.Exceptions;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using PKSim.Infrastructure.Services;
using ILazyLoadTask = PKSim.Core.Services.ILazyLoadTask;
using IObservedDataTask = PKSim.Core.Services.IObservedDataTask;
using IContainer = OSPSuite.Utility.Container.IContainer;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Utils.Extensions;
using OSPSuite.Core.Import;
using OSPSuite.Presentation.Services;

namespace PKSim.Infrastructure
{
   public abstract class concern_for_ImportObservedDataTask: ContextSpecification<IImportObservedDataTask>
   {
      protected IParameterIdentificationTask _parameterIdentificationTask;

      protected override void Context()
      {
         var _dataImporter = A.Fake<IDataImporter>();
         var _executionContext = A.Fake<IExecutionContext>();
         var _buildingBlockRepository = A.Fake<IBuildingBlockRepository>();
         var _speciesRepository = A.Fake<ISpeciesRepository>();
         var _defaultIndividualRetriever = A.Fake<IDefaultIndividualRetriever>();
         var _representationInfoRepository = A.Fake<IRepresentationInfoRepository>();
         var _observedDataTask = A.Fake<IObservedDataTask>();
         var _parameterChangeUpdater = A.Fake<IParameterChangeUpdater>();
         var _dialogCreator = A.Fake<IDialogCreator>();
         var _container = A.Fake<IContainer>();
         var _modelingXmlSerializerRepository = A.Fake<IOSPSuiteXmlSerializerRepository>();
         var _eventPublisher = A.Fake<IEventPublisher>();
         _parameterIdentificationTask = A.Fake<IParameterIdentificationTask>();

         var overwrittenData = new List<DataRepository>
         {
            A.Fake<DataRepository>(),
         };

         A.CallTo(() => _dataImporter.CalculateReloadDataSetsFromConfiguration(A<IReadOnlyList<DataRepository>>.Ignored, A<IReadOnlyList<DataRepository>>.Ignored))
            .Returns(new ReloadDataSets(new List<DataRepository>(),overwrittenData, new List<DataRepository>() ));

         sut = new ImportObservedDataTask(_dataImporter, _executionContext, _buildingBlockRepository, _speciesRepository,
            _defaultIndividualRetriever, _representationInfoRepository, _observedDataTask, _parameterChangeUpdater,
            _dialogCreator, _container, _modelingXmlSerializerRepository, _eventPublisher, _parameterIdentificationTask);
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