namespace BuildSoft.Command.DirSize.Table;

public enum ColumnType
{
    String = 0x100,
    Integer = 0x200,
    Int32 = Integer | 0x01,
    Int64 = Integer | 0x02,
}