using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MinCostFlow.Tests;

public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _category;
    public XunitLogger(ITestOutputHelper output, string category) { _output = output; _category = category; }
    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}");
        if (exception != null) _output.WriteLine(exception.ToString());
    }
}