using PartnershipManager.Domain.Enums;
using PartnershipManager.Application.Features.Financial.DTOs;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Infrastructure.Services.Financial;

namespace PartnershipManager.Tests.Unit.Application.Services;

public class FinancialMetricConfigServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IFinancialPeriodRepository> _periods = new();
    private readonly Mock<IFinancialMetricRepository> _metrics = new();
    private readonly Mock<IFinancialConfigRepository> _config = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly FinancialMetricConfigService _service;

    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public FinancialMetricConfigServiceTests()
    {
        _uow.SetupGet(x => x.FinancialPeriods).Returns(_periods.Object);
        _uow.SetupGet(x => x.FinancialMetrics).Returns(_metrics.Object);
        _uow.SetupGet(x => x.Companies).Returns(_companies.Object);
        _service = new FinancialMetricConfigService(_uow.Object, _config.Object);
    }

    private static FinancialMetricDefinition Def(string key, string dataType = "number", bool system = false, bool calc = false) =>
        new()
        {
            ClientId = ClientId, CompanyId = CompanyId, MetricKey = key, Label = key,
            DataType = dataType, IsSystem = system, IsCalculated = calc
        };

    private void SetupDefinitions(params FinancialMetricDefinition[] defs) =>
        _config.Setup(x => x.GetDefinitionsAsync(ClientId, CompanyId, true)).ReturnsAsync(defs);

    [Fact]
    public async Task CreateDefinition_GeneratesKeyFromLabel()
    {
        _companies.Setup(x => x.GetByIdAsync(CompanyId))
            .ReturnsAsync(Company.Create(ClientId, "Acme", "11222333000181", LegalForm.LTDA, DateTime.UtcNow.AddYears(-1), 1000, 1));
        SetupDefinitions(Def("mrr", system: true));

        var result = await _service.CreateDefinitionAsync(ClientId, new CreateMetricDefinitionRequest
        {
            CompanyId = CompanyId,
            Label = "Tickets Atendidos / Dia",
            DataType = "integer"
        });

        result.Key.Should().Be("tickets_atendidos_dia");
        _config.Verify(x => x.AddDefinitionAsync(It.Is<FinancialMetricDefinition>(d => d.MetricKey == "tickets_atendidos_dia")), Times.Once);
    }

    [Fact]
    public async Task CreateDefinition_DuplicateKey_Throws()
    {
        _companies.Setup(x => x.GetByIdAsync(CompanyId))
            .ReturnsAsync(Company.Create(ClientId, "Acme", "11222333000181", LegalForm.LTDA, DateTime.UtcNow.AddYears(-1), 1000, 1));
        SetupDefinitions(Def("mrr", system: true));

        var act = () => _service.CreateDefinitionAsync(ClientId, new CreateMetricDefinitionRequest
        {
            CompanyId = CompanyId, Label = "MRR", DataType = "currency"
        });

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task SaveValues_CustomMetric_PersistsOnlyInValuesTable()
    {
        var custom = Def("tickets_dia", "integer");
        var period = FinancialPeriod.Create(ClientId, CompanyId, 2025, 3);
        SetupDefinitions(custom);
        _periods.Setup(x => x.GetByIdAsync(period.Id, ClientId)).ReturnsAsync(period);
        _config.Setup(x => x.GetValuesByPeriodAsync(period.Id, ClientId)).ReturnsAsync(
            [new FinancialMetricValue { PeriodId = period.Id, DefinitionId = custom.Id, Value = 42 }]);

        var result = await _service.SavePeriodValuesAsync(period.Id, ClientId,
            new SavePeriodMetricValuesRequest { Values = new() { ["tickets_dia"] = 42 } }, UserId);

        result.Values["tickets_dia"].Should().Be(42);
        _config.Verify(x => x.UpsertValueAsync(ClientId, period.Id, custom.Id, 42), Times.Once);
        _metrics.Verify(x => x.AddAsync(It.IsAny<FinancialMetric>()), Times.Never);
    }

    [Fact]
    public async Task SaveValues_SystemMetric_UpdatesLegacyColumnsAndCalculatesArr()
    {
        var period = FinancialPeriod.Create(ClientId, CompanyId, 2025, 3);
        SetupDefinitions(Def("mrr", "currency", system: true), Def("arr", "currency", system: true, calc: true));
        _periods.Setup(x => x.GetByIdAsync(period.Id, ClientId)).ReturnsAsync(period);
        _metrics.Setup(x => x.GetByPeriodAsync(period.Id, ClientId)).ReturnsAsync((FinancialMetric?)null);
        _config.Setup(x => x.GetValuesByPeriodAsync(period.Id, ClientId)).ReturnsAsync([]);

        var result = await _service.SavePeriodValuesAsync(period.Id, ClientId,
            new SavePeriodMetricValuesRequest { Values = new() { ["mrr"] = 1000 } }, UserId);

        result.Values["arr"].Should().Be(12000);
        _metrics.Verify(x => x.AddAsync(It.Is<FinancialMetric>(m => m.Mrr == 1000)), Times.Once);
    }

    [Fact]
    public async Task SaveValues_CalculatedOrUnknownKey_Throws()
    {
        var period = FinancialPeriod.Create(ClientId, CompanyId, 2025, 3);
        SetupDefinitions(Def("arr", "currency", system: true, calc: true));
        _periods.Setup(x => x.GetByIdAsync(period.Id, ClientId)).ReturnsAsync(period);

        var calc = () => _service.SavePeriodValuesAsync(period.Id, ClientId,
            new SavePeriodMetricValuesRequest { Values = new() { ["arr"] = 1 } }, UserId);
        var unknown = () => _service.SavePeriodValuesAsync(period.Id, ClientId,
            new SavePeriodMetricValuesRequest { Values = new() { ["nope"] = 1 } }, UserId);

        await calc.Should().ThrowAsync<ValidationException>();
        await unknown.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SaveValues_IntegerMetricWithFraction_Throws()
    {
        var period = FinancialPeriod.Create(ClientId, CompanyId, 2025, 3);
        SetupDefinitions(Def("tickets_dia", "integer"));
        _periods.Setup(x => x.GetByIdAsync(period.Id, ClientId)).ReturnsAsync(period);

        var act = () => _service.SavePeriodValuesAsync(period.Id, ClientId,
            new SavePeriodMetricValuesRequest { Values = new() { ["tickets_dia"] = 1.5m } }, UserId);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SaveDashboardConfig_UnknownMetric_Throws()
    {
        SetupDefinitions(Def("mrr", "currency", system: true));

        var act = () => _service.SaveDashboardConfigAsync(ClientId, new SaveDashboardConfigRequest
        {
            CompanyId = CompanyId,
            Widgets = [new DashboardWidgetDto { Title = "X", WidgetType = "line", MetricKeys = ["ghost"] }]
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SaveDashboardConfig_Valid_ReplacesWidgets()
    {
        SetupDefinitions(Def("mrr", "currency", system: true));

        var result = await _service.SaveDashboardConfigAsync(ClientId, new SaveDashboardConfigRequest
        {
            CompanyId = CompanyId,
            Widgets = [new DashboardWidgetDto { Title = "MRR", WidgetType = "kpi", MetricKeys = ["mrr"] }]
        });

        result.Widgets.Should().ContainSingle();
        _config.Verify(x => x.ReplaceWidgetsAsync(ClientId, CompanyId, It.IsAny<IEnumerable<FinancialDashboardWidget>>()), Times.Once);
    }

    [Fact]
    public async Task SaveDashboardConfig_MonthlyChart_Throws()
    {
        SetupDefinitions(Def("mrr", "currency", system: true));

        var act = () => _service.SaveDashboardConfigAsync(ClientId, new SaveDashboardConfigRequest
        {
            CompanyId = CompanyId,
            Widgets = [new DashboardWidgetDto { Title = "X", WidgetType = "line", MetricKeys = ["mrr"], Scope = "monthly" }]
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SaveDashboardConfig_InvalidAggregation_Throws()
    {
        SetupDefinitions(Def("mrr", "currency", system: true));

        var act = () => _service.SaveDashboardConfigAsync(ClientId, new SaveDashboardConfigRequest
        {
            CompanyId = CompanyId,
            Widgets = [new DashboardWidgetDto { Title = "X", WidgetType = "kpi", MetricKeys = ["mrr"], Aggregation = "median" }]
        });

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SaveDashboardConfig_AnnualSumKpi_KeepsScopeAndAggregation()
    {
        SetupDefinitions(Def("mrr", "currency", system: true));

        var result = await _service.SaveDashboardConfigAsync(ClientId, new SaveDashboardConfigRequest
        {
            CompanyId = CompanyId,
            Widgets = [new DashboardWidgetDto { Title = "MRR", WidgetType = "kpi", MetricKeys = ["mrr"], Scope = "annual", Aggregation = "sum" }]
        });

        result.Widgets.Single().Aggregation.Should().Be("sum");
        result.Widgets.Single().Scope.Should().Be("annual");
    }
}
