using FakeItEasy;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Commands.Core;
using PKSim.Core.Commands;
using PKSim.Core.Model;

namespace PKSim.Core
{
   public abstract class concern_for_AddSchemaItemToSchemaCommand : ContextSpecification<AddSchemaItemToSchemaCommand>
   {
      protected IExecutionContext _context;
      protected SchemaItem _schemaItem;
      protected Schema _schema;

      protected override void Context()
      {
         _context = A.Fake<IExecutionContext>();
         _schemaItem = A.Fake<SchemaItem>();
         _schema = A.Fake<Schema>();
         sut = new AddSchemaItemToSchemaCommand(_schemaItem, _schema, _context);
      }
   }

   public class When_executing_the_add_schema_item_to_schema_command : concern_for_AddSchemaItemToSchemaCommand
   {
      protected override void Because()
      {
         sut.Execute(_context);
      }

      [Observation]
      public void should_add_the_schema_item_to_the_schema()
      {
         A.CallTo(() => _schema.Add(_schemaItem)).MustHaveHappened();
      }
   }

   public class The_inverse_of_the_add_schema_item_to_schema_command : concern_for_AddSchemaItemToSchemaCommand
   {
      private ICommand<IExecutionContext> _result;

      protected override void Because()
      {
         _result = sut.InverseCommand(_context);
      }

      [Observation]
      public void should_be_a_remove_schema_item_from_schema_command()
      {
         _result.ShouldBeAnInstanceOf<RemoveSchemaItemFromSchemaCommand>();
      }

      [Observation]
      public void should_have_been_marked_as_inverse_for_the_add_command()
      {
         _result.IsInverseFor(sut).ShouldBeTrue();
      }
   }
}