using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace MinCostFlow.Tests
{
    public class DebugEmbeddedResources
    {
        private readonly ITestOutputHelper _output;

        public DebugEmbeddedResources(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ListAllEmbeddedResources()
        {
            var assembly = typeof(Problems.Loaders.EmbeddedResourceLoader).Assembly;
            var resources = assembly.GetManifestResourceNames();
            
            _output.WriteLine($"Assembly: {assembly.GetName().Name}");
            _output.WriteLine($"Found {resources.Length} embedded resources:");
            foreach (var resource in resources)
            {
                _output.WriteLine($"  - {resource}");
            }
        }
    }
}