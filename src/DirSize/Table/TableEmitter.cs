using System.Buffers;
using System.Text;

namespace BuildSoft.Command.DirSize.Table;

class TableEmitter<T>
{
    private readonly List<ColumnInfo<T>> _columns = new();
    public void RegisterColumn(ColumnInfo<T> columnInfo)
    {
        _columns.Add(columnInfo);
    }

    public IEnumerable<string> Emit(IEnumerable<T> values, EmitFormat format, bool isRequiredHeader = true)
    {
        if (format == EmitFormat.None)
        {
            format = EmitFormat.FixedLength;
        }

        int[] sizes = new int[_columns.Count];
        if (format == EmitFormat.FixedLength)
        {
            if (values is not ICollection<T>)
            {
                values = values.ToArray();
            }
            int size = values.Count();
            for (int i = 0; i < sizes.Length; i++)
            {
                var column = _columns[i];
                sizes[i] = Math.Max(column.Name.Length, _sizeCalculators[column.Type].Invoke(values, column));
            }
        }
        return isRequiredHeader
            ? EmitHeader(format, sizes).Concat(EmitRows(values, format, sizes))
            : EmitRows(values, format, sizes);
    }

    private IEnumerable<string> EmitHeader(EmitFormat format, int[] sizes)
    {
        StringBuilder builder = new();
        char separator = _separators[format];
        for (int i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            AppendValue(builder, column.Name, ColumnType.String, format, sizes[i]);
            builder.Append(separator);
        }

        builder.Length--;
        yield return builder.ToString();

        if (format != EmitFormat.FixedLength)
        {
            yield break;
        }

        builder.Clear();

        for (int i = 0; i < _columns.Count; i++)
        {
            AppendValue(builder, new string('-', sizes[i]), ColumnType.String, format, sizes[i]);
            builder.Append(separator);
        }
        yield return builder.ToString();
    }

    private IEnumerable<string> EmitRows(IEnumerable<T> values, EmitFormat format, int[] sizes)
    {
        if (_columns.Count <= 0)
        {
            return values.Select(x => string.Empty);
        }
        return EnumerateRows(values, format, sizes);
    }

    private static readonly Dictionary<EmitFormat, char> _separators = new()
    {
        { EmitFormat.None,        ' '  },
        { EmitFormat.Csv,         ','  },
        { EmitFormat.Tsv,         '\t' },
        { EmitFormat.FixedLength, ' '  },
    };
    private static readonly Dictionary<ColumnType, Func<IEnumerable<T>, ColumnInfo<T>, int>> _sizeCalculators = new()
    {
        { ColumnType.String,  (v, c) => v.Any() ? v.Select(v=> GetFixedStringLength(c.GetValueFrom(v))).Max() : 0 },
        { ColumnType.Integer, (v, c) => v.Any() ? v.Select(v=> GetFixedStringLength(c.GetValueFrom(v))).Max() : 0 },
        { ColumnType.Int64,   (v, c) => 20 /* long.MinValue.ToString().Length */ },
        { ColumnType.Int32,   (v, c) => 11 /* int.MinValue.ToString().Length  */ },
    };

    private IEnumerable<string> EnumerateRows(IEnumerable<T> values, EmitFormat format, int[] sizes)
    {
        StringBuilder builder = new();
        char separator = _separators[format];
        foreach (T value in values)
        {
            builder.Clear();
            for (int i = 0; i < _columns.Count; i++)
            {
                var column = _columns[i];
                AppendValue(builder, column.GetValueFrom(value), column.Type, format, sizes[i]);
                builder.Append(separator);
            }
            builder.Length--;
            yield return builder.ToString();
        }
    }

    private static void AppendValue(StringBuilder builder, string value, ColumnType type, EmitFormat format, int fixedSize)
    {
        bool isNeedToTypeFormat = format == EmitFormat.Csv || format == EmitFormat.Tsv;
        bool isStringType = type.HasFlag(ColumnType.String);
        bool isIntegerType = type.HasFlag(ColumnType.Integer);

        int fixedValueLength = GetFixedStringLength(value);

        if (isNeedToTypeFormat && isStringType)
        {
            builder.Append('\"');
        }
        if (isIntegerType && format == EmitFormat.FixedLength)
        {
            builder.Append(' ', Math.Max(0, fixedSize - fixedValueLength));
        }
        builder.Append(GetEscapedString(value, format));
        if (isStringType && format == EmitFormat.FixedLength)
        {
            builder.Append(' ', Math.Max(0, fixedSize - fixedValueLength));
        }
        if (isNeedToTypeFormat && isStringType)
        {
            builder.Append('\"');
        }
    }

    private static int GetFixedStringLength(string s) => s.Length;
    private static string GetEscapedString(string s, EmitFormat format)
    {
        if (format == EmitFormat.Csv)
        {
            return s.Replace("\"", "\"\"");
        }
        return s;
    }
}