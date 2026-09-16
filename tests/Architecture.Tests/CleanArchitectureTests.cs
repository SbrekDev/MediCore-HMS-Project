using FluentAssertions;
using NetArchTest.Rules;

namespace MediCore.Architecture.Tests;

public class CleanArchitectureTests
{
    private const string DomainNamespace = "MediCore.Domain";
    private const string ApplicationNamespace = "MediCore.Application";
    private const string InfrastructureNamespace = "MediCore.Infrastructure";
    private const string ApiNamespace = "MediCore.Api";

    private static System.Reflection.Assembly DomainAssembly => typeof(MediCore.Domain.DomainAssemblyMarker).Assembly;
    private static System.Reflection.Assembly ApplicationAssembly => typeof(MediCore.Application.ApplicationAssemblyMarker).Assembly;
    private static System.Reflection.Assembly ApiAssembly => typeof(MediCore.Api.ApiAssemblyMarker).Assembly;

    [Fact]
    public void Domain_Should_Exist_And_Not_Depend_On_Other_Projects()
    {
        var domainTypes = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace(DomainNamespace)
            .GetTypes();

        domainTypes.Should().NotBeEmpty("Domain layer must contain types");

        var result = Types.InAssembly(DomainAssembly)
            .That().ResideInNamespace(DomainNamespace)
            .Should().NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain layer must not depend on Application, Infrastructure or Api layers");
    }

    [Fact]
    public void Application_Should_Exist_And_Not_Depend_On_Infrastructure_Or_Api()
    {
        var appTypes = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace(ApplicationNamespace)
            .GetTypes();

        appTypes.Should().NotBeEmpty("Application layer must contain types");

        var result = Types.InAssembly(ApplicationAssembly)
            .That().ResideInNamespace(ApplicationNamespace)
            .Should().NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application layer must not depend on Infrastructure or Api layers");
    }

    [Fact]
    public void Api_Should_Depend_On_Application_And_Not_Domain_Directly()
    {
        var apiTypes = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace(ApiNamespace)
            .GetTypes();

        apiTypes.Should().NotBeEmpty("Api layer must contain types");

        var apiTypesWithApplication = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace(ApiNamespace)
            .And().HaveDependencyOnAny(ApplicationNamespace)
            .GetTypes();

        apiTypesWithApplication.Should().NotBeEmpty("Api layer must reference Application layer");

        var apiTypesWithDomain = Types.InAssembly(ApiAssembly)
            .That().ResideInNamespace(ApiNamespace)
            .And().HaveDependencyOn(DomainNamespace)
            .GetTypes();

        apiTypesWithDomain.Should().BeEmpty(
            "Api layer must not reference Domain layer directly; go through Application");
    }
}
