using BuildSoft.Command.DirSize.Table;

namespace BuildSoft.Command.DirSize;

internal class DirSizeParameters : Parameters
{
    private string? _targetDirectory = null;

    public string RootDirectory => _targetDirectory ?? @".\";
    public bool IsRecursively { get; private set; } = false;
    public bool IsSingleTask { get; private set; } = false;
    public EmitFormat EmitFormat { get; private set; } = EmitFormat.None;
    public bool IsSilent { get; private set; } = false;
    public bool IsIncludingSubDirectoriesSize { get; private set; } = true;
    public bool IsNeedToAnalyzeRecursively => IsIncludingSubDirectoriesSize || IsRecursively;

    public DirSizeParameters(IEnumerable<string> parameters) : base(parameters, true)
    {

    }

    protected override string? ReadNonOptionParameter(ParamInfo parameter)
    {
        if (_targetDirectory == null && parameter.Index == 0)
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
                EmitFormat = EmitFormat.Csv;
                return null;

            case "tsv":
                EmitFormat = EmitFormat.Tsv;
                return null;

            case "top":
            case "t":
                IsIncludingSubDirectoriesSize = false;
                return null;
        }
        return $"{parameter.Content} is a invalid parameter.";
    }
}