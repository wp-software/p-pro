using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PartnershipManager.API.Middlewares;
using PartnershipManager.Domain.Exceptions;

namespace PartnershipManager.Tests.Unit.API;

public class SessionIdleTimeoutMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ActiveUserWithSession_ContinuesRequest()
    {
        var user = CreateActiveUser();
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Users).Returns(repository.Object);
        var nextCalled = false;
        var middleware = new SessionIdleTimeoutMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedContext(user.Id);

        await middleware.InvokeAsync(context, unitOfWork.Object);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_InactiveUser_RejectsRequest()
    {
        var user = CreateActiveUser();
        user.Deactivate();
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Users).Returns(repository.Object);
        var middleware = new SessionIdleTimeoutMiddleware(_ => Task.CompletedTask);

        var act = () => middleware.InvokeAsync(CreateAuthenticatedContext(user.Id), unitOfWork.Object);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task InvokeAsync_ExpiredSession_RejectsRequest()
    {
        var user = CreateActiveUser();
        user.SetRefreshToken("expired-refresh-token", days: -1);
        var repository = new Mock<IUserRepository>();
        repository.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Users).Returns(repository.Object);
        var middleware = new SessionIdleTimeoutMiddleware(_ => Task.CompletedTask);

        var act = () => middleware.InvokeAsync(CreateAuthenticatedContext(user.Id), unitOfWork.Object);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    private static User CreateActiveUser()
    {
        var user = User.Create(Guid.NewGuid(), "session@example.com", "Session User", "hash");
        user.Activate();
        user.SetRefreshToken("stored-refresh-token");
        return user;
    }

    private static HttpContext CreateAuthenticatedContext(Guid userId)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test"));
        return context;
    }
}
