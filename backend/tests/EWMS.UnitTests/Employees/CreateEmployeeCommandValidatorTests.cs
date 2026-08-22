using EWMS.Application.Employees.Commands.CreateEmployee;
using EWMS.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace EWMS.UnitTests.Employees;

public class CreateEmployeeCommandValidatorTests
{
    private readonly CreateEmployeeCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_EmployeeCode_Is_Empty()
    {
        var command = new CreateEmployeeCommand(
            "", "John", "Doe", "john@example.com", "9876543210", Gender.Male,
            new DateOnly(1995, 1, 1), new DateOnly(2024, 1, 1), Guid.NewGuid(), Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmployeeCommand.EmployeeCode));
    }

    [Fact]
    public void Should_Fail_When_Email_Is_Invalid()
    {
        var command = new CreateEmployeeCommand(
            "EMP001", "John", "Doe", "not-an-email", "9876543210", Gender.Male,
            new DateOnly(1995, 1, 1), new DateOnly(2024, 1, 1), Guid.NewGuid(), Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmployeeCommand.Email));
    }

    [Fact]
    public void Should_Pass_For_Valid_Command()
    {
        var command = new CreateEmployeeCommand(
            "EMP001", "John", "Doe", "john@example.com", "9876543210", Gender.Male,
            new DateOnly(1995, 1, 1), new DateOnly(2024, 1, 1), Guid.NewGuid(), Guid.NewGuid(), null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
