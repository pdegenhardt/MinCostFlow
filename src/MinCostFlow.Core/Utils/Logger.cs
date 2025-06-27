namespace MinCostFlow.Core.Utils;

public interface ILogger
{
    void Log(string message);
}

public sealed class NoOpLogger : ILogger
{
    public void Log(string message) { }
}
