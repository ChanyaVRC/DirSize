namespace BuildSoft.Command.DirSize;

internal class DirSizeParameters : Parameters
{
    private string? _targetDirectory = null;

    public string RootDirectory => _targetDirectory!;
    public bool IsRecursively { get; private set; } = false;
    public bool IsSingleTask { get; private set; } = false;
    public bool IsOutputAsCsv { get; private set; } = false;
    public bool IsSilent { get; private set; } = false;

    public DirSizeParameters(IEnumerable<string> parameters) : base(parameters, false)
    {

    }

    protected override string? ReadNonOptionParameter(ParamInfo parameter)
    {
        if (_targetDirectory == null)
        {
            _targetDirectory = Path.GetFullPath(parameter.Content);
            IsRequestOption = true;
            return null;
        }
        return $"{parameter.Content} is a invalid parameter.";
    }

    protected override string? ReadOptionParameter(ParamInfo parameter)
    {
        switch (parameter.Content[1..].ToLowerInvariant())
        {
            //case "help":
            //case "?":
            //    return "help";

            case "r":
                IsRecursively = true;
                return null;

            case "s":
                IsSingleTask = true;
                return null;

            case "q":
                IsSilent = true;
                return null;

            case "csv":
                IsOutputAsCsv = true;
                return null;
        }
        return $"{parameter.Content} is a invalid parameter.";
    }
}