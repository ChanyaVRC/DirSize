using BuildSoft.Command.DirSize.Logging;

namespace BuildSoft.Command.DirSize;

public abstract class Parameters
{
    public Parameters(IEnumerable<string> parameters, bool isRequestOptionAtFirst = true, ILogger? logger = null)
        : this(parameters.ToArray(), isRequestOptionAtFirst, logger)
    {

    }

    public Parameters(string[] parameters, bool isRequestOptionAtFirst = true, ILogger? logger = null)
    {
        IsRequestOption = isRequestOptionAtFirst;
        logger ??= new ConsoleLogger();

        for (int i = 0; i < parameters.Length; i++)
        {
            string? resultMessage = ReadCommand(new(parameters[i], i));
            if (resultMessage != null)
            {
                logger.Log(resultMessage);
            }
        }
    }

    protected bool IsRequestOption { get; set; }

    protected internal virtual string? ReadCommand(ParamInfo parameter)
    {
        return parameter.IsOption && IsRequestOption
            ? ReadOptionParameter(parameter)
            : ReadNonOptionParameter(parameter);
    }

    protected abstract string? ReadOptionParameter(ParamInfo parameter);

    protected abstract string? ReadNonOptionParameter(ParamInfo parameter);
}