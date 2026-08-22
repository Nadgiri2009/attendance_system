using EWMS.Application.Attendance.Commands.CheckIn;
using FluentAssertions;
using Xunit;

namespace EWMS.UnitTests.Attendance;

public class CheckInCommandValidatorTests
{
    private readonly CheckInCommandValidator _validator = new();

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Should_Fail_For_Out_Of_Range_Coordinates(double lat, double lng)
    {
        var command = new CheckInCommand(Guid.NewGuid(), lat, lng, 10, false, null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_For_Valid_Coordinates()
    {
        var command = new CheckInCommand(Guid.NewGuid(), 19.0760, 72.8777, 10, false, "Mumbai");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
