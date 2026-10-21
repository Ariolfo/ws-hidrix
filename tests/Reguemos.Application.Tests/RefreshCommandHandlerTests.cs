using FluentAssertions;
using Reguemos.Application.Common.Exceptions;
using Reguemos.Application.Common.Interfaces;
using Reguemos.Application.Features.Auth.Commands.Refresh;
using Moq;

namespace Reguemos.Application.Tests;

public class RefreshCommandHandlerTests
{
    [Fact]
    public async Task Handle_RejectsRefresh_WhenUserInactive()
    {
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.ValidateToken("valid-refresh", "refresh"))
            .Returns(("user-1", "Test User"));

        var identity = new Mock<IIdentityService>();
        identity.Setup(i => i.CanRefreshAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new RefreshCommandHandler(jwt.Object, identity.Object);

        var act = async () => await handler.Handle(
            new RefreshCommand { RefreshToken = "valid-refresh" },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAppException>()
            .WithMessage("*Sesión expirada*");
    }

    [Fact]
    public async Task Handle_IssuesTokens_WhenUserActive()
    {
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.ValidateToken("valid-refresh", "refresh"))
            .Returns(("user-1", "Test User"));
        jwt.Setup(j => j.CreateAccessToken("user-1", "Test User", It.IsAny<string[]>()))
            .Returns("new-access");
        jwt.Setup(j => j.CreateRefreshToken("user-1", "Test User"))
            .Returns("new-refresh");

        var identity = new Mock<IIdentityService>();
        identity.Setup(i => i.CanRefreshAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        identity.Setup(i => i.GetUserRolesAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(["User"]);

        var handler = new RefreshCommandHandler(jwt.Object, identity.Object);

        var result = await handler.Handle(
            new RefreshCommand { RefreshToken = "valid-refresh" },
            CancellationToken.None);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
    }
}
