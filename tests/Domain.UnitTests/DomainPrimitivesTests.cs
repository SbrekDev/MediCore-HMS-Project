using FluentAssertions;
using MediCore.Domain.Common;
using MediCore.Domain.Errors;

namespace MediCore.Domain.UnitTests;

public class DomainPrimitivesTests
{
    [Fact]
    public void Entity_equality_is_based_on_id()
    {
        var entity1 = new TestEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var entity2 = new TestEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var entity3 = new TestEntity(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        entity1.Should().Be(entity2);
        entity1.Should().NotBe(entity3);
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity3).Should().BeTrue();
    }

    [Fact]
    public void Result_success_holds_value()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Result_failure_holds_error()
    {
        var error = DomainError.NotFound("Entity.Missing", "Entity was not found");
        var result = Result<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Result_failure_throws_on_value_access()
    {
        var error = DomainError.Validation("Entity.Invalid", "Invalid");
        var result = Result<int>.Failure(error);

        result.Invoking(r => _ = r.Value)
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AuditableEntity_captures_created_and_modified_metadata()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new TestAuditableEntity
        {
            CreatedAt = now,
            CreatedBy = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ModifiedAt = now,
            ModifiedBy = Guid.Parse("33333333-3333-3333-3333-333333333333"),
        };

        entity.CreatedAt.Should().Be(now);
        entity.CreatedBy.Should().Be(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }

    private sealed class TestAuditableEntity : AuditableEntity<Guid> { }
}
