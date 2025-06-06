using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PKSim.Assets;
using PKSim.Core;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using PKSim.Presentation.Presenters.Formulations;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Data;
using OSPSuite.Core.Domain.Formulas;
using OSPSuite.Presentation.Core;
using OSPSuite.Assets;
using IFormulaFactory = PKSim.Core.Model.IFormulaFactory;
using OSPSuite.Core.Extensions;
using OSPSuite.Infrastructure.Import.Core;
using OSPSuite.Infrastructure.Import.Services;
using OSPSuite.Core.Services;

namespace PKSim.Presentation.Services
{
   public interface IFormulationTask : IBuildingBlockTask<Formulation>
   {
      Formulation CreateFormulationForRoute(string applicationRoute);
      Task<Formulation> LoadFormulationForRouteAsync(string applicationRoute);

      DataRepository ImportTablePoints();
      TableFormula TableFormulaFrom(DataRepository dataRepository);
   }

   public class FormulationTask : BuildingBlockTask<Formulation>, IFormulationTask
   {
      public IDimensionRepository DimensionRepository { get; set; }
      private readonly IDataImporter _dataImporter;
      private readonly IDimensionRepository _dimensionRepository;
      private readonly IFormulaFactory _formulaFactory;
      private readonly IDialogCreator _dialogCreator;

      public FormulationTask(IExecutionContext executionContext, IBuildingBlockTask buildingBlockTask, IApplicationController applicationController, IDataImporter dataImporter,
         IDimensionRepository dimensionRepository, IFormulaFactory formulaFactory, IDialogCreator dialogCreator)
         : base(executionContext, buildingBlockTask, applicationController, PKSimBuildingBlockType.Formulation)
      {
         DimensionRepository = dimensionRepository;
         _dataImporter = dataImporter;
         _dimensionRepository = dimensionRepository;
         _formulaFactory = formulaFactory;
         _dialogCreator = dialogCreator;
      }

      public override Formulation AddToProject()
      {
         //no need to specify route when adding a default formulation
         return AddToProject<ICreateFormulationPresenter>();
      }

      public Formulation CreateFormulationForRoute(string applicationRoute)
      {
         return AddToProject<ICreateFormulationPresenter>(x => x.CreateFormulation(applicationRoute));
      }

      public async Task<Formulation> LoadFormulationForRouteAsync(string applicationRoute)
      {
         var formulation = await LoadSingleFromTemplateAsync();
         if (formulation == null)
            return null;

         if (formulation.HasRoute(applicationRoute))
            return formulation;

         throw new FormulationCannotBeUsedForRouteException(formulation, applicationRoute);
      }

      public DataRepository ImportTablePoints()
      {
         var dataImporterSettings = new DataImporterSettings
         {
            Caption = $"{CoreConstants.ProductDisplayName} - {PKSimConstants.UI.ImportFormulation}",
            IconName = ApplicationIcons.Formulation.IconName
         };
         dataImporterSettings.AddNamingPatternMetaData(Constants.FILE);

         return _dataImporter.ImportDataSets(
            new List<MetaDataCategory>(), 
            getColumnInfos(), 
            dataImporterSettings,
            _dialogCreator.AskForFileToOpen(Captions.Importer.OpenFile, Captions.Importer.ImportFileFilter, Constants.DirectoryKey.OBSERVED_DATA)
         ).DataRepositories.FirstOrDefault();
      }

      public TableFormula TableFormulaFrom(DataRepository dataRepository)
      {
         var baseGrid = dataRepository.BaseGrid;
         var valueColumn = dataRepository.AllButBaseGrid().Single();
         var formula = _formulaFactory.CreateTableFormula().WithName(dataRepository.Name);
         formula.InitializedWith(Constants.TIME, dataRepository.Name, baseGrid.Dimension, valueColumn.Dimension);
         formula.XDisplayUnit = baseGrid.Dimension.Unit(baseGrid.DataInfo.DisplayUnitName);
         formula.YDisplayUnit = valueColumn.Dimension.Unit(valueColumn.DataInfo.DisplayUnitName);

         foreach (var timeValue in baseGrid.Values)
         {
            formula.AddPoint(timeValue, valueColumn.GetValue(timeValue).ToDouble());
         }
         return formula;
      }

      private IReadOnlyList<ColumnInfo> getColumnInfos()
      {
         var columns = new List<ColumnInfo>();

         var timeColumn = new ColumnInfo
         {
            DefaultDimension = _dimensionRepository.Time,
            Name = PKSimConstants.UI.Time,
            DisplayName = PKSimConstants.UI.Time,
            IsMandatory = true,
         };


         timeColumn.SupportedDimensions.Add(_dimensionRepository.Time);
         columns.Add(timeColumn);

         var fractionColumn = new ColumnInfo
         {
            DefaultDimension = _dimensionRepository.Fraction,
            Name = PKSimConstants.UI.Fraction,
            DisplayName = PKSimConstants.UI.Fraction,
            IsMandatory = true,
            BaseGridName = timeColumn.Name,
         };

         fractionColumn.SupportedDimensions.Add(_dimensionRepository.Fraction);
         columns.Add(fractionColumn);

         return columns;
      }
   }
}