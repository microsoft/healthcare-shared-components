// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using EnsureThat;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using Microsoft.Health.SqlServer.Features.Storage;

namespace Microsoft.Health.SqlServer.Features.Schema.Model;

#pragma warning disable SA1402 // File may only contain a single type. Adding all Column-derived types in this files.

/// <summary>
/// Represents a table or table type column
/// </summary>
public abstract class Column
{
    protected Column(string name, SqlDbType type, bool nullable)
        : this(nullable)
    {
        Metadata = new SqlMetaData(name, type);
    }

    protected Column(string name, SqlDbType type, bool nullable, long length)
        : this(nullable)
    {
        Metadata = new SqlMetaData(name, type, length);
    }

    protected Column(string name, SqlDbType type, bool nullable, byte precision, byte scale)
        : this(nullable)
    {
        Metadata = new SqlMetaData(name, type, precision, scale);
    }

    protected Column(string name, SqlDbType dbType, bool nullable, long length, byte precision, byte scale, long locale, SqlCompareOptions compareOptions, Type userDefinedType)
        : this(nullable)
    {
        Metadata = new SqlMetaData(name, dbType, length, precision, scale, locale, compareOptions, userDefinedType);
    }

    private Column(bool nullable)
    {
        Nullable = nullable;
    }

    public bool Nullable { get; }

    public SqlMetaData Metadata { get; }

    public static implicit operator string(Column column)
    {
        EnsureArg.IsNotNull(column, nameof(column));
        return column.ToString();
    }

    public override string ToString() => Metadata.Name;
}

/// <summary>
/// Represents a typed table or table type column
/// </summary>
/// <typeparam name="T">The CLR column type</typeparam>
public abstract class Column<T> : Column
{
    protected Column(string name, SqlDbType type, bool nullable)
        : base(name, type, nullable)
    {
    }

    protected Column(string name, SqlDbType type, bool nullable, long length)
        : base(name, type, nullable, length)
    {
    }

    protected Column(string name, SqlDbType type, bool nullable, byte precision, byte scale)
        : base(name, type, nullable, precision, scale)
    {
    }

    protected Column(string name, SqlDbType dbType, bool nullable, long length, byte precision, byte scale, long locale, SqlCompareOptions compareOptions, Type userDefinedType)
        : base(name, dbType, nullable, length, precision, scale, locale, compareOptions, userDefinedType)
    {
    }

    public abstract T Read(SqlDataReader reader, int ordinal);

    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Method name will not change at this time.")]
    public abstract void Set(SqlDataRecord record, int ordinal, T value);
}

public class IntColumn : Column<int>
{
    public IntColumn(string name)
        : base(name, SqlDbType.Int, false)
    {
    }

    public override int Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetInt32(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, int value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetInt32(ordinal, value);
    }
}

public class BigIntColumn : Column<long>
{
    public BigIntColumn(string name)
        : base(name, SqlDbType.BigInt, false)
    {
    }

    public override long Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetInt64(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, long value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetInt64(ordinal, value);
    }
}

public class BitColumn : Column<bool>
{
    public BitColumn(string name)
        : base(name, SqlDbType.Bit, false)
    {
    }

    public override bool Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetBoolean(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, bool value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetBoolean(ordinal, value);
    }
}

public class DateTimeColumn : Column<DateTime>
{
    public DateTimeColumn(string name)
        : base(name, SqlDbType.DateTime, false)
    {
    }

    public override DateTime Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetDateTime(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTime value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetDateTime(ordinal, value);
    }
}

public class NullableDateTimeColumn : Column<DateTime?>
{
    public NullableDateTimeColumn(string name)
        : base(name, SqlDbType.DateTime, true)
    {
    }

    public override DateTime? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(DateTime?) : reader.GetDateTime(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTime? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value == null)
        {
            record.SetDBNull(ordinal);
        }
        else
        {
            record.SetDateTime(ordinal, value.Value);
        }
    }
}

public class DateTime2Column : Column<DateTime>
{
    public DateTime2Column(string name, byte scale)
        : base(name, SqlDbType.DateTime2, false, 0, scale)
    {
    }

    public override DateTime Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetDateTime(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTime value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetDateTime(ordinal, value);
    }
}

public class NullableDateColumn : Column<DateTime?>
{
    public NullableDateColumn(string name)
        : base(name, SqlDbType.DateTime2, true, 0, 0)
    {
    }

    public override DateTime? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(DateTime?) : reader.GetDateTime(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTime? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value == null)
        {
            record.SetDBNull(ordinal);
        }
        else
        {
            record.SetDateTime(ordinal, value.Value);
        }
    }
}

public class DateTimeOffsetColumn : Column<DateTimeOffset>
{
    public DateTimeOffsetColumn(string name, byte scale)
        : base(name, SqlDbType.DateTimeOffset, false, 0, scale)
    {
    }

    public override DateTimeOffset Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetDateTimeOffset(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTimeOffset value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetDateTimeOffset(ordinal, value);
    }
}

public class DecimalColumn : Column<decimal>
{
    public DecimalColumn(string name, byte precision, byte scale)
        : base(name, SqlDbType.Decimal, false, precision, scale)
    {
        MinValue = SqlMetadataUtilities.GetMinValueForDecimalColumn(Metadata);
        MaxValue = SqlMetadataUtilities.GetMaxValueForDecimalColumn(Metadata);
    }

    public decimal MinValue { get; }

    public decimal MaxValue { get; }

    public override decimal Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetDecimal(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, decimal value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        // TODO: restore the validation once we handle the error appropriately on the FHIR server, user story 92202
        // ColumnUtilities.ValidateLength(Metadata, value);

        record.SetDecimal(ordinal, value);
    }
}

public class FloatColumn : Column<double>
{
    public FloatColumn(string name)
        : base(name, SqlDbType.Float, false)
    {
    }

    public FloatColumn(string name, byte precision)
        : base(name, SqlDbType.Float, false, ColumnUtilities.GetLengthForFloatColumn(precision), precision, 0, 0, SqlCompareOptions.None, null)
    {
    }

    public override double Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetDouble(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, double value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetDouble(ordinal, value);
    }
}

public class SmallIntColumn : Column<short>
{
    public SmallIntColumn(string name)
        : base(name, SqlDbType.SmallInt, false)
    {
    }

    public override short Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetInt16(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, short value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetInt16(ordinal, value);
    }
}

public class TinyIntColumn : Column<byte>
{
    public TinyIntColumn(string name)
        : base(name, SqlDbType.TinyInt, false)
    {
    }

    public override byte Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetByte(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, byte value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetByte(ordinal, value);
    }
}

public class UniqueIdentifierColumn : Column<Guid>
{
    public UniqueIdentifierColumn(string name)
        : base(name, SqlDbType.UniqueIdentifier, false)
    {
    }

    public override Guid Read(SqlDataReader reader, int ordinal)
    {
        return reader.GetGuid(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, Guid value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        record.SetGuid(ordinal, value);
    }
}

public class NullableIntColumn : Column<int?>
{
    public NullableIntColumn(string name)
        : base(name, SqlDbType.Int, true)
    {
    }

    public override int? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(int?) : reader.GetInt32(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, int? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetInt32(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableBigIntColumn : Column<long?>
{
    public NullableBigIntColumn(string name)
        : base(name, SqlDbType.BigInt, true)
    {
    }

    public override long? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(long?) : reader.GetInt64(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, long? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetInt64(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableBitColumn : Column<bool?>
{
    public NullableBitColumn(string name)
        : base(name, SqlDbType.Bit, true)
    {
    }

    public override bool? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(bool?) : reader.GetBoolean(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, bool? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetBoolean(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableDateTime2Column : Column<DateTime?>
{
    public NullableDateTime2Column(string name, byte scale)
        : base(name, SqlDbType.DateTime2, true, 0, scale)
    {
    }

    public override DateTime? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(DateTime?) : reader.GetDateTime(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTime? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetDateTime(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableDateTimeOffsetColumn : Column<DateTimeOffset?>
{
    public NullableDateTimeOffsetColumn(string name, byte scale)
        : base(name, SqlDbType.DateTimeOffset, true, 0, scale)
    {
    }

    public override DateTimeOffset? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(DateTimeOffset?) : reader.GetDateTimeOffset(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, DateTimeOffset? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetDateTimeOffset(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableDecimalColumn : Column<decimal?>
{
    public NullableDecimalColumn(string name, byte precision, byte scale)
        : base(name, SqlDbType.Decimal, true, precision, scale)
    {
        MinValue = SqlMetadataUtilities.GetMinValueForDecimalColumn(Metadata);
        MaxValue = SqlMetadataUtilities.GetMaxValueForDecimalColumn(Metadata);
    }

    public decimal MinValue { get; }

    public decimal MaxValue { get; }

    public override decimal? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(decimal?) : reader.GetDecimal(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, decimal? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            // TODO: restore the validation once we handle the error appropriately on the FHIR server, user story 92202
            //ColumnUtilities.ValidateLength(Metadata, value.Value);

            record.SetDecimal(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableFloatColumn : Column<double?>
{
    public NullableFloatColumn(string name)
        : base(name, SqlDbType.Float, true)
    {
    }
    public NullableFloatColumn(string name, byte precision)
        : base(name, SqlDbType.Float, true, ColumnUtilities.GetLengthForFloatColumn(precision), precision, 0, 0, SqlCompareOptions.None, null)
    {
    }

    public override double? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(double?) : reader.GetDouble(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, double? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetDouble(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableSmallIntColumn : Column<short?>
{
    public NullableSmallIntColumn(string name)
        : base(name, SqlDbType.SmallInt, true)
    {
    }

    public override short? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(short?) : reader.GetInt16(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, short? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetInt16(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableTinyIntColumn : Column<byte?>
{
    public NullableTinyIntColumn(string name)
        : base(name, SqlDbType.TinyInt, true)
    {
    }

    public override byte? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(byte?) : reader.GetByte(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, byte? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetByte(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableUniqueIdentifierColumn : Column<Guid?>
{
    public NullableUniqueIdentifierColumn(string name)
        : base(name, SqlDbType.UniqueIdentifier, true)
    {
    }

    public override Guid? Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? default(Guid?) : reader.GetGuid(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, Guid? value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value.HasValue)
        {
            record.SetGuid(ordinal, value.Value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

/// <summary>
/// Represents a rowversion type column.
/// </summary>
/// <remarks>
/// The timestamp data type is a synonym for the rowversion data type, and its syntax is now deprecated in SQL.
/// The rowversion data type is used in the SQL code, and it is translated to timestamp on the C# side because
/// the SqlDbType enum only supports timestamp.
/// </remarks>
public class TimestampColumn : Column<byte[]>
{
    public TimestampColumn(string name)
        : base(name, SqlDbType.Timestamp, nullable: false) // Values in the rowversion column can never be null.
    {
    }

    public override byte[] Read(SqlDataReader reader, int ordinal)
    {
        // The rowversion storage size is 8 bytes.
        const int length = 8;

        byte[] bytes = new byte[length];

        reader.GetBytes(Metadata.Name, ordinal, fieldOffset: 0, bytes, bufferOffset: 0, length);
        return bytes;
    }

    public override void Set(SqlDataRecord record, int ordinal, byte[] value)
    {
        EnsureArg.IsNotNull(record, nameof(record));
        EnsureArg.IsNotNull(value, nameof(value));
        record.SetBytes(ordinal, fieldOffset: 0, value, bufferOffset: 0, value.Length);
    }
}

public class BinaryColumn : Column<byte[]>
{
    public BinaryColumn(string name, int length)
        : base(name, SqlDbType.Binary, true, length)
    {
        Length = length;
    }

    public int Length { get; }

    public override byte[] Read(SqlDataReader reader, int ordinal)
    {
        EnsureArg.IsNotNull(reader, nameof(reader));

        byte[] bytes = new byte[Length];

        if (Nullable && reader.IsDBNull(ordinal))
        {
            return null;
        }
        else
        {
            reader.GetBytes(Metadata.Name, ordinal, fieldOffset: 0, bytes, bufferOffset: 0, Length);
            return bytes;
        }
    }

    public override void Set(SqlDataRecord record, int ordinal, byte[] value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value != null)
        {
            record.SetBytes(ordinal, fieldOffset: 0, value, bufferOffset: 0, value.Length);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public abstract class StringColumn : Column<string>
{
    protected StringColumn(string name, SqlDbType type, bool nullable, int length, string collation = null)
        : base(name, type, nullable, length)
    {
        Collation = collation;
        if (collation != null)
        {
            IsAcentSensitive = collation.Contains("_AS", StringComparison.OrdinalIgnoreCase);
            IsCaseSensitive = collation.Contains("_CS", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string Collation { get; }

    public bool? IsCaseSensitive { get; }

    public bool? IsAcentSensitive { get; }

    public override string Read(SqlDataReader reader, int ordinal)
    {
        EnsureArg.IsNotNull(reader, nameof(reader));
        return Nullable && reader.IsDBNull(ordinal) ? null : reader.GetString(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, string value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value != null)
        {
            record.SetString(ordinal, value);
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NVarCharColumn : StringColumn
{
    public NVarCharColumn(string name, int length, string collation = null)
        : base(name, SqlDbType.NVarChar, false, length, collation)
    {
    }
}

public class CharColumn : StringColumn
{
    public CharColumn(string name, int length, string collation = null)
        : base(name, SqlDbType.Char, false, length, collation)
    {
    }
}

public class VarCharColumn : StringColumn
{
    public VarCharColumn(string name, int length, string collation = null)
        : base(name, SqlDbType.VarChar, false, length, collation)
    {
    }
}

public class VarBinaryColumn : Column<Stream>
{
    public VarBinaryColumn(string name, int length)
        : this(name, false, length)
    {
    }

    public VarBinaryColumn(string name, bool nullable, int length)
        : base(name, SqlDbType.VarBinary, nullable, length)
    {
    }

    public override Stream Read(SqlDataReader reader, int ordinal)
    {
        return Nullable && reader.IsDBNull(Metadata.Name, ordinal) ? Stream.Null : reader.GetStream(Metadata.Name, ordinal);
    }

    public override void Set(SqlDataRecord record, int ordinal, Stream value)
    {
        EnsureArg.IsNotNull(record, nameof(record));

        if (value != null)
        {
            if (!record.IsDBNull(ordinal))
            {
                // Clear old value before setting new value.
                record.SetDBNull(ordinal);
            }

            int length = (int)value.Length;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    value.CopyTo(ms, length);
                }

                record.SetBytes(ordinal, 0, bytes, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
        else
        {
            record.SetDBNull(ordinal);
        }
    }
}

public class NullableNVarCharColumn : StringColumn
{
    public NullableNVarCharColumn(string name, int length, string collation = null)
        : base(name, SqlDbType.NVarChar, true, length, collation)
    {
    }
}

public class NullableVarCharColumn : StringColumn
{
    public NullableVarCharColumn(string name, int length, string collation = null)
        : base(name, SqlDbType.VarChar, true, length, collation)
    {
    }
}

public class NullableVarBinaryColumn : VarBinaryColumn
{
    public NullableVarBinaryColumn(string name, int length)
        : base(name, nullable: true, length)
    {
    }

    public override Stream Read(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(Metadata.Name, ordinal) ? null : reader.GetStream(Metadata.Name, ordinal);
    }
}

#pragma warning restore SA1402 // File may only contain a single type
