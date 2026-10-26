using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartnershipManager.API.Controllers;
using PartnershipManager.Application.Features.Users.DTOs;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces.Services;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.Tests.Unit.API;

public class UsersControllerAccessTests
{
    [Fact]
    public async Task GetById_AdminFromDifferentClient_ThrowsForbidden()
    {
        var fixture = CreateFixture(Role.Admin.ToString());
        var target = User.Create(Guid.NewGuid(), "target@example.com", "Target", "hash", fixture.CompanyId);
        fixture.Users.Setup(x => x.GetByIdAsync(target.Id)).ReturnsAsync(target);

        var act = () => fixture.Controller.GetById(target.Id);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Update_AdminFromDifferentCompanyInSameClient_ThrowsForbidden()
    {
        var fixture = CreateFixture(Role.Admin.ToString());
        var target = User.Create(fixture.ClientId, "target@example.com", "Target", "hash", Guid.NewGuid());
        fixture.Users.Setup(x => x.GetByIdAsync(target.Id)).ReturnsAsync(target);

        var act = () => fixture.Controller.Update(target.Id, new UpdateUserRequest { Name = "Changed" });

        await act.Should().ThrowAsync<ForbiddenException>();
        fixture.Users.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task AddRole_AdminCannotAssignSuperAdmin()
    {
        var fixture = CreateFixture(Role.Admin.ToString());
        var target = User.Create(fixture.ClientId, "target@example.com", "Target", "hash", fixture.CompanyId);
        fixture.Users.Setup(x => x.GetByIdAsync(target.Id)).ReturnsAsync(target);

        var act = () => fixture.Controller.AddRole(target.Id, new ManageUserRoleRequest { Role = Role.SuperAdmin });

        await act.Should().ThrowAsync<ForbiddenException>();
        fixture.UserRoles.Verify(x => x.AddAsync(It.IsAny<UserRole>()), Times.Never);
    }

    [Fact]
    public async Task AddRole_AdminCannotChangeOwnRoles()
    {
        var fixture = CreateFixture(Role.Admin.ToString());
        var currentUser = User.Create(fixture.ClientId, "admin@example.com", "Admin", "hash", fixture.CompanyId);
        fixture.CurrentUser.SetupGet(x => x.UserId).Returns(currentUser.Id);
        fixture.Users.Setup(x => x.GetByIdAsync(currentUser.Id)).ReturnsAsync(currentUser);

        var act = () => fixture.Controller.AddRole(currentUser.Id, new ManageUserRoleRequest { Role = Role.Finance });

        await act.Should().ThrowAsync<ForbiddenException>();
        fixture.UserRoles.Verify(x => x.AddAsync(It.IsAny<UserRole>()), Times.Never);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.SuperAdmin)]
    public async Task Create_HrCannotAssignPrivilegedRoles(Role role)
    {
        var fixture = CreateFixture(Role.HR.ToString());

        var act = () => fixture.Controller.Create(new CreateUserRequest
        {
            Name = "New User",
            Email = "new@example.com",
            InitialRole = role
        });

        await act.Should().ThrowAsync<ForbiddenException>();
        fixture.Users.Verify(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid>(), null), Times.Never);
    }

    private static ControllerFixture CreateFixture(params string[] roles)
    {
        var clientId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var users = new Mock<IUserRepository>();
        var userRoles = new Mock<IUserRoleRepository>();
        userRoles.Setup(x => x.GetRoleNamesByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<string>());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Users).Returns(users.Object);
        unitOfWork.SetupGet(x => x.UserRoles).Returns(userRoles.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.ClientId).Returns(clientId);
        currentUser.SetupGet(x => x.CompanyId).Returns(companyId);
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        currentUser.SetupGet(x => x.Roles).Returns(roles);

        var controller = new UsersController(
            unitOfWork.Object,
            Mock.Of<IAuthService>(),
            Mock.Of<ICacheService>(),
            currentUser.Object,
            Mock.Of<IEmailService>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<UsersController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        return new ControllerFixture(controller, users, userRoles, currentUser, clientId, companyId);
    }

    private sealed record ControllerFixture(
        UsersController Controller,
        Mock<IUserRepository> Users,
        Mock<IUserRoleRepository> UserRoles,
        Mock<ICurrentUserService> CurrentUser,
        Guid ClientId,
        Guid CompanyId);
}
