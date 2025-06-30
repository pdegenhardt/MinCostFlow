using MinCostFlow.Core.Utils;
using Xunit.Abstractions;

namespace MinCostFlow.Tests;

public class XunitTestLogger : ILogger
{
    private readonly ITestOutputHelper _output;

    public XunitTestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    public void Log(string message)
    {
        _output.WriteLine(message);
    }
}