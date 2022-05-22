namespace BuildSoft.Command.DirSize.Logging;

public interface ILogger
{
    void Log() => Log(string.Empty);
    void Log(string message);
}