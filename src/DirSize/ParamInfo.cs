namespace BuildSoft.Command.DirSize;

public record struct ParamInfo
{
    private readonly string _content;

    public bool IsOption => _content.Length > 1 && (_content[0] == '-' || _content[0] == '/');
    public string Content => _content;
    public int Index { get; }

    public ParamInfo(string param, int index)
    {
        _content = param;
        Index = index;
    }
}