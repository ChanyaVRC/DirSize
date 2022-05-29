namespace BuildSoft.Command.DirSize.Table;

public record struct ColumnInfo<T>(string Name, ColumnType Type, Func<T, string> ValueExtractor)
{
    public string GetValueFrom(T row) => ValueExtractor.Invoke(row);
}