using Longa.Application.Common.Interfaces;
using Longa.Application.Services;
using Moq;
using Xunit;

namespace Longa.UnitTests;

public class HealthServiceTests
{
    [Fact]
    public void GetStatus_ReturnsOkWithTimestamp()
    {
        var expectedTime = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.UtcNow).Returns(expectedTime);

        var sut = new HealthService(dateTimeMock.Object);

        var result = sut.GetStatus();

        Assert.Equal("ok", result.Status);
        Assert.Equal(expectedTime, result.Timestamp);
    }
}
