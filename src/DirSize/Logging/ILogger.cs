namespace BuildSoft.Command.DirSize.Logging;

interface ILogger
{
    void Log() => Log(string.Empty);
    void Log(string message);
}