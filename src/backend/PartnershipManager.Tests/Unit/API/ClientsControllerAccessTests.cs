using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartnershipManager.API.Controllers;
using PartnershipManager.Application.Features.Clients.DTOs;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.Tests.Unit.API;

public class ClientsControllerAccessTests
{
    private static ClientsController CreateController(
        string role, Guid? clientId, Mock<ICoreClientRepository>? repo = null, Mock<IUnitOfWork>? uow = null)
    {
        var controller = new ClientsController(
            (repo ?? new Mock<ICoreClientRepository>()).Object,
            (uow ?? new Mock<IUnitOfWork>()).Object,
            Mock.Of<ICacheService>(),
            Mock.Of<IEmailService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<ClientsController>>());

        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (clientId.HasValue) claims.Add(new Claim("clientId", clientId.Value.ToString()));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };
        return controller;
    }

    [Fact]
    public async Task Update_AdminOfAnotherClient_ThrowsForbidden()
    {
        var controller = CreateController("Admin", Guid.NewGuid());

        var act = () => controller.Update(Guid.NewGuid(), new UpdateClientRequest { Name = "X" });

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetById_AdminOfAnotherClient_ThrowsForbidden()
    {
        var controller = CreateController("Admin", null);

        var act = () => controller.GetById(Guid.NewGuid());

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetClientUsers_UnknownClient_ThrowsNotFound()
    {
        var repo = new Mock<ICoreClientRepository>();
        repo.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);
        var controller = CreateController("SuperAdmin", null, repo);

        var act = () => controller.GetClientUsers(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetClientUsers_ExistingClient_ReturnsPagedUsers()
    {
        var clientId = Guid.NewGuid();
        var repo = new Mock<ICoreClientRepository>();
        repo.Setup(x => x.ExistsAsync(clientId)).ReturnsAsync(true);
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetPagedByClientAsync(clientId, 1, 10, null))
            .ReturnsAsync((new List<User>(), 0));
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(x => x.Users).Returns(users.Object);
        var controller = CreateController("SuperAdmin", null, repo, uow);

        var result = await controller.GetClientUsers(clientId);

        result.Should().BeOfType<OkObjectResult>();
        users.Verify(x => x.GetPagedByClientAsync(clientId, 1, 10, null), Times.Once);
    }

    [Fact]
    public async Task CreateClientUser_CompanyFromAnotherClient_ThrowsNotFound()
    {
        var clientId = Guid.NewGuid();
        var repo = new Mock<ICoreClientRepository>();
        repo.Setup(x => x.ExistsAsync(clientId)).ReturnsAsync(true);
        var companies = new Mock<ICompanyRepository>();
        companies.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Company?)null);
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(x => x.Companies).Returns(companies.Object);
        var controller = CreateController("SuperAdmin", null, repo, uow);

        var act = () => controller.CreateClientUser(clientId,
            new CreateClientUserRequest { Name = "A", Email = "a@a.com", CompanyId = Guid.NewGuid() });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateClientUser_SuperAdminRole_ThrowsForbidden()
    {
        var clientId = Guid.NewGuid();
        var repo = new Mock<ICoreClientRepository>();
        repo.Setup(x => x.ExistsAsync(clientId)).ReturnsAsync(true);
        var controller = CreateController("SuperAdmin", null, repo);

        var act = () => controller.CreateClientUser(clientId,
            new CreateClientUserRequest { Name = "A", Email = "a@a.com", Role = PartnershipManager.Domain.Enums.Role.SuperAdmin });

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
