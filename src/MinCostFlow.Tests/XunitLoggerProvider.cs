using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MinCostFlow.Tests;

// XunitLoggerProvider implementation
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;
    public XunitLoggerProvider(ITestOutputHelper output) { _output = output; }
    public ILogger CreateLogger(string categoryName) => new XunitLogger(_output, categoryName);
    public void Dispose() { }
}
