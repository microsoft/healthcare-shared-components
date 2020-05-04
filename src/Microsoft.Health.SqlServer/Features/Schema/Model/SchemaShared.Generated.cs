//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Microsoft.Health.SqlServer.Features.Schema.Model
{
    using Microsoft.Health.SqlServer.Features.Schema.Model;

    internal class SchemaShared
    {
        internal readonly static InstanceSchemaTable InstanceSchema = new InstanceSchemaTable();
        internal readonly static SchemaVersionTable SchemaVersion = new SchemaVersionTable();
        internal readonly static DeleteInstanceSchemaProcedure DeleteInstanceSchema = new DeleteInstanceSchemaProcedure();
        internal readonly static GetInstanceSchemaByNameProcedure GetInstanceSchemaByName = new GetInstanceSchemaByNameProcedure();
        internal readonly static SelectCompatibleSchemaVersionsProcedure SelectCompatibleSchemaVersions = new SelectCompatibleSchemaVersionsProcedure();
        internal readonly static SelectCurrentVersionsInformationProcedure SelectCurrentVersionsInformation = new SelectCurrentVersionsInformationProcedure();
        internal readonly static UpsertInstanceSchemaProcedure UpsertInstanceSchema = new UpsertInstanceSchemaProcedure();
        internal class InstanceSchemaTable : Table
        {
            internal InstanceSchemaTable(): base("dbo.InstanceSchema")
            {
            }

            internal readonly VarCharColumn Name = new VarCharColumn("Name", 64, "Latin1_General_100_CS_AS");
            internal readonly IntColumn CurrentVersion = new IntColumn("CurrentVersion");
            internal readonly IntColumn MaxVersion = new IntColumn("MaxVersion");
            internal readonly IntColumn MinVersion = new IntColumn("MinVersion");
            internal readonly DateTime2Column Timeout = new DateTime2Column("Timeout", 0);
        }

        internal class SchemaVersionTable : Table
        {
            internal SchemaVersionTable(): base("dbo.SchemaVersion")
            {
            }

            internal readonly IntColumn Version = new IntColumn("Version");
            internal readonly VarCharColumn Status = new VarCharColumn("Status", 10);
        }

        internal class DeleteInstanceSchemaProcedure : StoredProcedure
        {
            internal DeleteInstanceSchemaProcedure(): base("dbo.DeleteInstanceSchema")
            {
            }

            public void PopulateCommand(global::System.Data.SqlClient.SqlCommand command)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.DeleteInstanceSchema";
            }
        }

        internal class GetInstanceSchemaByNameProcedure : StoredProcedure
        {
            internal GetInstanceSchemaByNameProcedure(): base("dbo.GetInstanceSchemaByName")
            {
            }

            private readonly ParameterDefinition<System.String> _name = new ParameterDefinition<System.String>("@name", global::System.Data.SqlDbType.VarChar, false, 64);
            public void PopulateCommand(global::System.Data.SqlClient.SqlCommand command, System.String name)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.GetInstanceSchemaByName";
                _name.AddParameter(command.Parameters, name);
            }
        }

        internal class SelectCompatibleSchemaVersionsProcedure : StoredProcedure
        {
            internal SelectCompatibleSchemaVersionsProcedure(): base("dbo.SelectCompatibleSchemaVersions")
            {
            }

            public void PopulateCommand(global::System.Data.SqlClient.SqlCommand command)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.SelectCompatibleSchemaVersions";
            }
        }

        internal class SelectCurrentVersionsInformationProcedure : StoredProcedure
        {
            internal SelectCurrentVersionsInformationProcedure(): base("dbo.SelectCurrentVersionsInformation")
            {
            }

            public void PopulateCommand(global::System.Data.SqlClient.SqlCommand command)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.SelectCurrentVersionsInformation";
            }
        }

        internal class UpsertInstanceSchemaProcedure : StoredProcedure
        {
            internal UpsertInstanceSchemaProcedure(): base("dbo.UpsertInstanceSchema")
            {
            }

            private readonly ParameterDefinition<System.String> _name = new ParameterDefinition<System.String>("@name", global::System.Data.SqlDbType.VarChar, false, 64);
            private readonly ParameterDefinition<System.Int32> _maxVersion = new ParameterDefinition<System.Int32>("@maxVersion", global::System.Data.SqlDbType.Int, false);
            private readonly ParameterDefinition<System.Int32> _minVersion = new ParameterDefinition<System.Int32>("@minVersion", global::System.Data.SqlDbType.Int, false);
            private readonly ParameterDefinition<System.Int32> _addMinutesOnTimeout = new ParameterDefinition<System.Int32>("@addMinutesOnTimeout", global::System.Data.SqlDbType.Int, false);
            public void PopulateCommand(global::System.Data.SqlClient.SqlCommand command, System.String name, System.Int32 maxVersion, System.Int32 minVersion, System.Int32 addMinutesOnTimeout)
            {
                command.CommandType = global::System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.UpsertInstanceSchema";
                _name.AddParameter(command.Parameters, name);
                _maxVersion.AddParameter(command.Parameters, maxVersion);
                _minVersion.AddParameter(command.Parameters, minVersion);
                _addMinutesOnTimeout.AddParameter(command.Parameters, addMinutesOnTimeout);
            }
        }
    }
}