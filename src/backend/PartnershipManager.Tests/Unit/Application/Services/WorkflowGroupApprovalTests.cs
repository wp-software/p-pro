using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PartnershipManager.Application.Services;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Interfaces.Services;

namespace PartnershipManager.Tests.Unit.Application.Services;

public class WorkflowGroupApprovalTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid WorkflowId = Guid.NewGuid();
    private static readonly Guid StepId = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly Guid Outsider = Guid.NewGuid();

    private readonly Mock<IWorkflowRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly WorkflowService _service;

    public WorkflowGroupApprovalTests()
    {
        _uow.SetupGet(x => x.Users).Returns(new Mock<IUserRepository>().Object);
        _service = new WorkflowService(
            _repo.Object,
            new Mock<IApprovalGroupRepository>().Object,
            new Mock<INotificationService>().Object,
            new Mock<IEmailService>().Object,
            _uow.Object,
            NullLogger<WorkflowService>.Instance,
            new ConfigurationBuilder().Build());
    }

    private void SetupWorkflow(string mode, params Guid[] alreadyApproved)
    {
        var step = new WorkflowStep
        {
            Id = StepId,
            WorkflowId = WorkflowId,
            StepOrder = 1,
            IsCurrent = true,
            Status = WorkflowStepStatuses.InProgress,
            AssignedGroupId = Guid.NewGuid(),
            ApprovalMode = mode,
            Approvers = { new WorkflowStepApprover { UserId = UserA }, new WorkflowStepApprover { UserId = UserB } }
        };
        foreach (var id in alreadyApproved)
            step.Approvals.Add(new WorkflowApproval { UserId = id, Decision = WorkflowDecisions.Approved });

        _repo.Setup(r => r.GetByIdAsync(WorkflowId, CompanyId)).ReturnsAsync(new Workflow
        {
            Id = WorkflowId,
            CompanyId = CompanyId,
            Status = WorkflowStatuses.InProgress,
            CurrentStep = 1,
            TotalSteps = 1,
            Steps = { step }
        });
    }

    [Fact]
    public async Task Approve_AnyMode_OneMemberCompletesWorkflow()
    {
        SetupWorkflow(ApprovalModes.Any);

        await _service.ApproveStepAsync(WorkflowId, StepId, CompanyId, UserA);

        _repo.Verify(r => r.UpdateStepStatusAsync(StepId, WorkflowStepStatuses.Completed, UserA), Times.Once);
        _repo.Verify(r => r.CompleteWorkflowAsync(WorkflowId, WorkflowStatuses.Approved), Times.Once);
    }

    [Fact]
    public async Task Approve_AllMode_FirstMemberOnlyRecordsVote()
    {
        SetupWorkflow(ApprovalModes.All);

        await _service.ApproveStepAsync(WorkflowId, StepId, CompanyId, UserA);

        _repo.Verify(r => r.RecordApprovalAsync(It.IsAny<WorkflowApproval>()), Times.Once);
        _repo.Verify(r => r.UpdateStepStatusAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
        _repo.Verify(r => r.CompleteWorkflowAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Approve_AllMode_LastMemberCompletesWorkflow()
    {
        SetupWorkflow(ApprovalModes.All, UserA);

        await _service.ApproveStepAsync(WorkflowId, StepId, CompanyId, UserB);

        _repo.Verify(r => r.CompleteWorkflowAsync(WorkflowId, WorkflowStatuses.Approved), Times.Once);
    }

    [Fact]
    public async Task Approve_NonMember_IsForbidden()
    {
        SetupWorkflow(ApprovalModes.Any);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.ApproveStepAsync(WorkflowId, StepId, CompanyId, Outsider));
    }

    [Fact]
    public async Task Approve_MemberAlreadyVoted_Throws()
    {
        SetupWorkflow(ApprovalModes.All, UserA);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ApproveStepAsync(WorkflowId, StepId, CompanyId, UserA));
    }

    [Fact]
    public async Task Reject_AllMode_AnyMemberRejectsWorkflow()
    {
        SetupWorkflow(ApprovalModes.All);

        await _service.RejectStepAsync(WorkflowId, StepId, CompanyId, UserA, "não");

        _repo.Verify(r => r.CompleteWorkflowAsync(WorkflowId, WorkflowStatuses.Rejected), Times.Once);
    }
}
