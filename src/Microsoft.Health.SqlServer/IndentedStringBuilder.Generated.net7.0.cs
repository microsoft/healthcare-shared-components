//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Microsoft.Health.SqlServer
{
    public partial class IndentedStringBuilder
    {
        private readonly System.Text.StringBuilder _inner;

        public IndentedStringBuilder(System.Text.StringBuilder inner)
        {
            _inner = inner ?? throw new System.ArgumentNullException(nameof(inner));
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Int32 Capacity
        {
            get
            {
                return _inner.Capacity;
            }

            set
            {
                _inner.Capacity = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Char this[System.Int32 index]
        {
            get
            {
                return _inner[index];
            }

            set
            {
                _inner[index] = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Int32 Length
        {
            get
            {
                return _inner.Length;
            }

            set
            {
                _inner.Length = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Int32 MaxCapacity
        {
            get
            {
                return _inner.MaxCapacity;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Boolean value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Byte value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Char value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Char value, System.Int32 repeatCount)
        {
            AppendIndent();
            _inner.Append(value, repeatCount);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Char[] value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Char[] value, System.Int32 startIndex, System.Int32 charCount)
        {
            AppendIndent();
            _inner.Append(value, startIndex, charCount);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Decimal value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Double value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Int16 value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Int32 value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Int64 value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Object value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.ReadOnlyMemory<System.Char> value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.ReadOnlySpan<System.Char> value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.SByte value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Single value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.String value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.String value, System.Int32 startIndex, System.Int32 count)
        {
            AppendIndent();
            _inner.Append(value, startIndex, count);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Text.StringBuilder value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.Text.StringBuilder value, System.Int32 startIndex, System.Int32 count)
        {
            AppendIndent();
            _inner.Append(value, startIndex, count);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.UInt16 value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.UInt32 value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Append(System.UInt64 value)
        {
            AppendIndent();
            _inner.Append(value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat(System.IFormatProvider provider, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, System.Object arg0)
        {
            AppendIndent();
            _inner.AppendFormat(provider, format, arg0);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat(System.IFormatProvider provider, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, System.Object arg0, System.Object arg1)
        {
            AppendIndent();
            _inner.AppendFormat(provider, format, arg0, arg1);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat(System.IFormatProvider provider, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, System.Object arg0, System.Object arg1, System.Object arg2)
        {
            AppendIndent();
            _inner.AppendFormat(provider, format, arg0, arg1, arg2);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat(System.IFormatProvider provider, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, params System.Object[] args)
        {
            AppendIndent();
            _inner.AppendFormat(provider, format, args);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, System.Object arg0)
        {
            AppendIndent();
            _inner.AppendFormat(format, arg0);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, System.Object arg0, System.Object arg1)
        {
            AppendIndent();
            _inner.AppendFormat(format, arg0, arg1);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, System.Object arg0, System.Object arg1, System.Object arg2)
        {
            AppendIndent();
            _inner.AppendFormat(format, arg0, arg1, arg2);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendFormat([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] System.String format, params System.Object[] args)
        {
            AppendIndent();
            _inner.AppendFormat(format, args);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendJoin<T>(System.Char separator, System.Collections.Generic.IEnumerable<T> values)
        {
            AppendIndent();
            _inner.AppendJoin<T>(separator, values);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendJoin(System.Char separator, params System.Object[] values)
        {
            AppendIndent();
            _inner.AppendJoin(separator, values);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendJoin(System.Char separator, params System.String[] values)
        {
            AppendIndent();
            _inner.AppendJoin(separator, values);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendJoin<T>(System.String separator, System.Collections.Generic.IEnumerable<T> values)
        {
            AppendIndent();
            _inner.AppendJoin<T>(separator, values);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendJoin(System.String separator, params System.Object[] values)
        {
            AppendIndent();
            _inner.AppendJoin(separator, values);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendJoin(System.String separator, params System.String[] values)
        {
            AppendIndent();
            _inner.AppendJoin(separator, values);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendLine()
        {
            AppendIndent();
            _inner.AppendLine();
            _indentPending = true;
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder AppendLine(System.String value)
        {
            AppendIndent();
            _inner.AppendLine(value);
            _indentPending = true;
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Clear()
        {
            _inner.Clear();
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public void CopyTo(System.Int32 sourceIndex, System.Char[] destination, System.Int32 destinationIndex, System.Int32 count)
        {
            _inner.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public void CopyTo(System.Int32 sourceIndex, System.Span<System.Char> destination, System.Int32 count)
        {
            _inner.CopyTo(sourceIndex, destination, count);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Int32 EnsureCapacity(System.Int32 capacity)
        {
            return _inner.EnsureCapacity(capacity);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public override System.Boolean Equals(System.Object obj)
        {
            return _inner.Equals(obj);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Boolean Equals(System.ReadOnlySpan<System.Char> span)
        {
            return _inner.Equals(span);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Boolean Equals(System.Text.StringBuilder sb)
        {
            return _inner.Equals(sb);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.Text.StringBuilder.ChunkEnumerator GetChunks()
        {
            return _inner.GetChunks();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public override System.Int32 GetHashCode()
        {
            return _inner.GetHashCode();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Boolean value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Byte value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Char value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Char[] value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Char[] value, System.Int32 startIndex, System.Int32 charCount)
        {
            _inner.Insert(index, value, startIndex, charCount);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Decimal value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Double value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Int16 value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Int32 value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Int64 value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Object value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.ReadOnlySpan<System.Char> value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.SByte value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.Single value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.String value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.String value, System.Int32 count)
        {
            _inner.Insert(index, value, count);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.UInt16 value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.UInt32 value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Insert(System.Int32 index, System.UInt64 value)
        {
            _inner.Insert(index, value);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Remove(System.Int32 startIndex, System.Int32 length)
        {
            _inner.Remove(startIndex, length);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Replace(System.Char oldChar, System.Char newChar)
        {
            _inner.Replace(oldChar, newChar);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Replace(System.Char oldChar, System.Char newChar, System.Int32 startIndex, System.Int32 count)
        {
            _inner.Replace(oldChar, newChar, startIndex, count);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Replace(System.String oldValue, System.String newValue)
        {
            _inner.Replace(oldValue, newValue);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IndentedStringBuilder Replace(System.String oldValue, System.String newValue, System.Int32 startIndex, System.Int32 count)
        {
            _inner.Replace(oldValue, newValue, startIndex, count);
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public override System.String ToString()
        {
            return _inner.ToString();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public System.String ToString(System.Int32 startIndex, System.Int32 length)
        {
            return _inner.ToString(startIndex, length);
        }
    }
}