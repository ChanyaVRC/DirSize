namespace BuildSoft.Command.DirSize;

public abstract class Parameters
{
    public Parameters(IEnumerable<string> parameters, bool isRequestOptionAtFirst = true)
        : this(parameters.ToArray(), isRequestOptionAtFirst)
    {

    }

    public Parameters(string[] parameters, bool isRequestOptionAtFirst = true)
    {
        IsRequestOption = isRequestOptionAtFirst;

        for (int i = 0; i < parameters.Length; i++)
        {
            string? resultMessage = ReadCommand(new(parameters[i], i));
            if (resultMessage != null)
            {
                Console.WriteLine(resultMessage);
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