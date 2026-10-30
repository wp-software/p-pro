using Moq;
using PartnershipManager.Application.Features.ShareClasses.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces;
using PartnershipManager.Infrastructure.Services;

namespace PartnershipManager.Tests.Unit.Infrastructure.Services;

public class ShareClassServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesClassForTheWholeTenant()
    {
        var clientId = Guid.NewGuid();
        var repository = new Mock<IShareClassRepository>();
        var companyRepository = new Mock<ICompanyRepository>();
        ShareClass? createdClass = null;

        var request = new CreateShareClassRequest
        {
            Name = "Ações ordinárias",
            Code = "ON"
        };

        repository
            .Setup(x => x.CodeExistsAsync(clientId, null, request.Code, null))
            .ReturnsAsync(false);
        repository
            .Setup(x => x.AddAsync(It.IsAny<ShareClass>()))
            .Callback<ShareClass>(shareClass => createdClass = shareClass)
            .Returns(Task.CompletedTask);
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), clientId))
            .Returns((Guid _, Guid _) => Task.FromResult(createdClass));

        var service = new ShareClassService(repository.Object, companyRepository.Object);

        var result = await service.CreateAsync(clientId, request);

        Assert.Null(createdClass!.CompanyId);
        Assert.Null(result.CompanyId);
        companyRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}
