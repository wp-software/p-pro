using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PartnershipManager.API.Controllers;
using PartnershipManager.Application.Features.Clients.DTOs;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.Tests.Unit.API;

public class ClientsControllerOnboardingTests
{
    [Fact]
    public async Task Onboard_ValidRequest_CreatesTenantCompanyAdminAndSendsInvitation()
    {
        var fixture = CreateFixture();

        var result = await fixture.Controller.Onboard(CreateRequest());

        result.Should().BeOfType<CreatedAtActionResult>();
        fixture.ClientRepository.Verify(x => x.AddAsync(It.IsAny<Client>()), Times.Once);
        fixture.CompanyRepository.Verify(x => x.AddAsync(It.IsAny<Company>()), Times.Once);
        fixture.UserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        fixture.UserRoleRepository.Verify(x => x.AddAsync(It.IsAny<UserRole>()), Times.Once);
        fixture.UnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
        fixture.UnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        fixture.UnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);
        fixture.EmailService.Verify(
            x => x.SendAccountActivationEmailAsync(
                "admin@example.com",
                "Admin User",
                It.Is<string>(link => link.Contains("/activate-account?token=")),
                "portuguese"),
            Times.Once);
    }

    [Fact]
    public async Task Onboard_CompanyCreationFails_RollsBackAndDoesNotSendInvitation()
    {
        var fixture = CreateFixture(companyCreationFails: true);

        var act = () => fixture.Controller.Onboard(CreateRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
        fixture.UnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
        fixture.UnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
        fixture.UnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        fixture.EmailService.Verify(
            x => x.SendAccountActivationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    private static OnboardClientRequest CreateRequest() => new()
    {
        Client = new CreateClientRequest
        {
            Name = "Tenant Example",
            Document = "11222333000181",
            DocumentType = DocumentType.Cnpj,
            Email = "contact@example.com"
        },
        Company = new()
        {
            Name = "Example Company Ltda",
            Cnpj = "11222333000181",
            LegalForm = LegalForm.LTDA,
            FoundationDate = new DateTime(2010, 1, 1),
            TotalShares = 1000,
            SharePrice = 1,
            Currency = "BRL"
        },
        AdministratorName = "Admin User",
        AdministratorEmail = "admin@example.com"
    };

    private static ControllerFixture CreateFixture(bool companyCreationFails = false)
    {
        var clientRepository = new Mock<ICoreClientRepository>();
        clientRepository.Setup(x => x.DocumentExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        clientRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        clientRepository.Setup(x => x.AddAsync(It.IsAny<Client>())).Returns(Task.CompletedTask);

        var companyRepository = new Mock<ICompanyRepository>();
        companyRepository.Setup(x => x.CnpjExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        if (companyCreationFails)
        {
            companyRepository.Setup(x => x.AddAsync(It.IsAny<Company>()))
                .ThrowsAsync(new InvalidOperationException("company insert failed"));
        }
        else
        {
            companyRepository.Setup(x => x.AddAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);
        }

        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), null)).ReturnsAsync((User?)null);
        userRepository.Setup(x => x.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        userRepository.Setup(x => x.UpdatePasswordResetTokenAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime?>())).Returns(Task.CompletedTask);

        var userRoleRepository = new Mock<IUserRoleRepository>();
        userRoleRepository.Setup(x => x.AddAsync(It.IsAny<UserRole>())).Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Companies).Returns(companyRepository.Object);
        unitOfWork.SetupGet(x => x.Users).Returns(userRepository.Object);
        unitOfWork.SetupGet(x => x.UserRoles).Returns(userRoleRepository.Object);
        unitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var emailService = new Mock<IEmailService>();
        emailService.Setup(x => x.SendAccountActivationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var controller = new ClientsController(
            clientRepository.Object,
            unitOfWork.Object,
            Mock.Of<ICacheService>(),
            emailService.Object,
            new ConfigurationBuilder().Build(),
            Mock.Of<ILogger<ClientsController>>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        return new ControllerFixture(
            controller,
            clientRepository,
            companyRepository,
            userRepository,
            userRoleRepository,
            unitOfWork,
            emailService);
    }

    private sealed record ControllerFixture(
        ClientsController Controller,
        Mock<ICoreClientRepository> ClientRepository,
        Mock<ICompanyRepository> CompanyRepository,
        Mock<IUserRepository> UserRepository,
        Mock<IUserRoleRepository> UserRoleRepository,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IEmailService> EmailService);
}