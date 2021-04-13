//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Microsoft.Health.SqlServer.Web.Features.Schema.Model
{
    using Microsoft.Health.SqlServer.Features.Client;
    using Microsoft.Health.SqlServer.Features.Schema.Model;

    internal class VLatest
    {
        internal readonly static MyViewView MyView = new MyViewView();
        internal readonly static Table1Table Table1 = new Table1Table();
        internal readonly static Table2Table Table2 = new Table2Table();
        internal readonly static InsertNumbersProcedure InsertNumbers = new InsertNumbersProcedure();
        internal readonly static MyProcedureProcedure MyProcedure = new MyProcedureProcedure();

        internal class MyViewView : Table
        {
            internal MyViewView() : base("dbo.MyView")
            {
            }

            internal readonly IntColumn Id = new IntColumn("Id");
            internal readonly NVarCharColumn Name = new NVarCharColumn("Name", 20);
            internal readonly NVarCharColumn TheCity = new NVarCharColumn("TheCity", 20);
            internal readonly Index IXC_View12 = new Index("IXC_View12");
            internal readonly Index IX_View12_City = new Index("IX_View12_City");
        }

        internal class Table1Table : Table
        {
            internal Table1Table() : base("dbo.Table1")
            {
            }

            internal readonly IntColumn Id = new IntColumn("Id");
            internal readonly NVarCharColumn Name = new NVarCharColumn("Name", 20);
        }

        internal class Table2Table : Table
        {
            internal Table2Table() : base("dbo.Table2")
            {
            }

            internal readonly IntColumn Id = new IntColumn("Id");
            internal readonly NVarCharColumn City = new NVarCharColumn("City", 20);
        }

        internal class InsertNumbersProcedure : StoredProcedure
        {
            internal InsertNumbersProcedure() : base("dbo.InsertNumbers")
            {
            }

            private readonly ComplexNumberV1TableValuedParameterDefinition _names = new ComplexNumberV1TableValuedParameterDefinition("@names");

            public void PopulateCommand(SqlCommandWrapper command, global::System.Collections.Generic.IEnumerable<ComplexNumberV1Row> names)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.InsertNumbers";
                _names.AddParameter(command.Parameters, names);
            }

            public void PopulateCommand(SqlCommandWrapper command, InsertNumbersTableValuedParameters tableValuedParameters)
            {
                PopulateCommand(command, names: tableValuedParameters.Names);
            }
        }

        internal class InsertNumbersTvpGenerator<TInput> : IStoredProcedureTableValuedParametersGenerator<TInput, InsertNumbersTableValuedParameters>
        {
            public InsertNumbersTvpGenerator(ITableValuedParameterRowGenerator<TInput, ComplexNumberV1Row> ComplexNumberV1RowGenerator)
            {
                this.ComplexNumberV1RowGenerator = ComplexNumberV1RowGenerator;
            }

            private readonly ITableValuedParameterRowGenerator<TInput, ComplexNumberV1Row> ComplexNumberV1RowGenerator;

            public InsertNumbersTableValuedParameters Generate(TInput input)
            {
                return new InsertNumbersTableValuedParameters(ComplexNumberV1RowGenerator.GenerateRows(input));
            }
        }

        internal struct InsertNumbersTableValuedParameters
        {
            internal InsertNumbersTableValuedParameters(global::System.Collections.Generic.IEnumerable<ComplexNumberV1Row> Names)
            {
                this.Names = Names;
            }

            internal global::System.Collections.Generic.IEnumerable<ComplexNumberV1Row> Names { get; }
        }

        internal class MyProcedureProcedure : StoredProcedure
        {
            internal MyProcedureProcedure() : base("dbo.MyProcedure_2")
            {
            }

            private readonly NameTypeV1TableValuedParameterDefinition _names = new NameTypeV1TableValuedParameterDefinition("@names");

            public void PopulateCommand(SqlCommandWrapper command, global::System.Collections.Generic.IEnumerable<NameTypeV1Row> names)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.MyProcedure_2";
                _names.AddParameter(command.Parameters, names);
            }

            public void PopulateCommand(SqlCommandWrapper command, MyProcedureTableValuedParameters tableValuedParameters)
            {
                PopulateCommand(command, names: tableValuedParameters.Names);
            }
        }

        internal class MyProcedureTvpGenerator<TInput> : IStoredProcedureTableValuedParametersGenerator<TInput, MyProcedureTableValuedParameters>
        {
            public MyProcedureTvpGenerator(ITableValuedParameterRowGenerator<TInput, NameTypeV1Row> NameTypeV1RowGenerator)
            {
                this.NameTypeV1RowGenerator = NameTypeV1RowGenerator;
            }

            private readonly ITableValuedParameterRowGenerator<TInput, NameTypeV1Row> NameTypeV1RowGenerator;

            public MyProcedureTableValuedParameters Generate(TInput input)
            {
                return new MyProcedureTableValuedParameters(NameTypeV1RowGenerator.GenerateRows(input));
            }
        }

        internal struct MyProcedureTableValuedParameters
        {
            internal MyProcedureTableValuedParameters(global::System.Collections.Generic.IEnumerable<NameTypeV1Row> Names)
            {
                this.Names = Names;
            }

            internal global::System.Collections.Generic.IEnumerable<NameTypeV1Row> Names { get; }
        }
    }
}