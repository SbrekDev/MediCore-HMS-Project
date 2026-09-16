using FluentAssertions;
using MediCore.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MediCore.Application.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_returns_same_service_collection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}
