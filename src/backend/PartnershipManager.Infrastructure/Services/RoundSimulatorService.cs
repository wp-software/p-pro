using PartnershipManager.Application.Features.Simulation.DTOs;
using PartnershipManager.Domain.Entities;
using PartnershipManager.Domain.Enums;
using PartnershipManager.Domain.Exceptions;
using PartnershipManager.Domain.Interfaces;

namespace PartnershipManager.Infrastructure.Services;

public interface IRoundSimulatorService
{
    /// <summary>
    /// Simula uma rodada de investimento e retorna o cap table projetado
    /// </summary>
    Task<RoundSimulationResponse> SimulateRoundAsync(Guid clientId, RoundSimulationRequest request);
    
    /// <summary>
    /// Calcula apenas a diluição sem detalhes completos
    /// </summary>
    Task<decimal> CalculateDilutionAsync(Guid clientId, Guid companyId, decimal investmentAmount, decimal preMoneyValuation);
}

public class RoundSimulatorService : IRoundSimulatorService
{
    private readonly IShareRepository _shareRepository;
    private readonly IShareholderRepository _shareholderRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IShareClassRepository _shareClassRepository;
    private readonly IVestingGrantRepository _vestingGrantRepository;

    public RoundSimulatorService(
        IShareRepository shareRepository,
        IShareholderRepository shareholderRepository,
        ICompanyRepository companyRepository,
        IShareClassRepository shareClassRepository,
        IVestingGrantRepository vestingGrantRepository)
    {
        _shareRepository = shareRepository;
        _shareholderRepository = shareholderRepository;
        _companyRepository = companyRepository;
        _shareClassRepository = shareClassRepository;
        _vestingGrantRepository = vestingGrantRepository;
    }


    public async Task<RoundSimulationResponse> SimulateRoundAsync(Guid clientId, RoundSimulationRequest request)
    {
        // Validações básicas
        if (request.PreMoneyValuation <= 0)
            throw new ValidationException("PreMoneyValuation", "Pre-money valuation deve ser maior que zero");
        
        if (request.InvestmentAmount <= 0)
            throw new ValidationException("InvestmentAmount", "Valor do investimento deve ser maior que zero");
        
        // Verificar se empresa existe
        var company = await _companyRepository.GetByIdAsync(request.CompanyId);
        if (company == null || company.ClientId != clientId)
            throw new NotFoundException("Company", request.CompanyId);

        // Obter cap table atual
        var currentShares = await _shareRepository.GetActiveByCompanyAsync(clientId, request.CompanyId);
        
        // Calcular totais atuais
        var totalSharesBefore = currentShares.Sum(s => s.Quantity);
        
        // Se não há ações, usar 1.000.000 como base (comum em startups)
        if (totalSharesBefore == 0)
        {
            totalSharesBefore = 1_000_000;
        }
        
        // Calcular preço por ação baseado no pre-money
        var pricePerShare = request.PreMoneyValuation / totalSharesBefore;
        
        // Calcular post-money valuation
        var postMoneyValuation = request.PreMoneyValuation + request.InvestmentAmount;

        // ── Lógica por tipo de aquisição ──────────────────────────────────────
        decimal newShares;
        decimal totalSharesAfter;
        decimal totalDilution;

        if (request.AcquisitionType == AcquisitionType.Secondary)
        {
            // Aquisição secundária: compra de ações existentes — total de ações não muda
            newShares = 0;
            totalSharesAfter = totalSharesBefore;
            totalDilution = 0;
        }
        else
        {
            // Emissão primária: novas ações são criadas
            newShares = request.InvestmentAmount / pricePerShare;
            totalSharesAfter = totalSharesBefore + newShares;
            totalDilution = 1 - (totalSharesBefore / totalSharesAfter);
        }
        
        // Calcular pool de opções se aplicável (apenas em emissão primária)
        OptionPoolInfo? optionPoolInfo = null;
        if (request.AcquisitionType == AcquisitionType.Primary &&
            request.IncludeOptionPool && request.OptionPoolPercentage > 0)
        {
            var poolShares = CalculateOptionPoolShares(
                totalSharesBefore, 
                newShares, 
                request.OptionPoolPercentage, 
                request.OptionPoolPreMoney);
            
            optionPoolInfo = new OptionPoolInfo
            {
                Percentage = request.OptionPoolPercentage,
                Shares = poolShares,
                IsPreMoney = request.OptionPoolPreMoney,
                Value = poolShares * pricePerShare
            };
            
            totalSharesAfter += poolShares;
            totalDilution = 1 - (totalSharesBefore / totalSharesAfter);
        }
        
        // Construir cap table antes
        var capTableBefore = await BuildCapTableEntries(currentShares, totalSharesBefore, pricePerShare);
        
        List<SimulatedShareholderEntry> capTableAfter;
        var newInvestors = new List<SimulatedNewInvestor>();

        if (request.AcquisitionType == AcquisitionType.Secondary)
        {
            // Secundária: investidores compram de holders existentes (pro-rata)
            var investorSharesTotal = request.InvestmentAmount / pricePerShare;
            
            // Todos os holders atuais vendem proporcionalmente
            capTableAfter = capTableBefore.Select(entry =>
            {
                var sharesSold = entry.Shares * (investorSharesTotal / totalSharesBefore);
                var sharesKept = entry.Shares - sharesSold;
                return entry with
                {
                    Shares = sharesKept,
                    Ownership = (sharesKept / totalSharesAfter) * 100,
                    Value = sharesKept * pricePerShare,
                    DilutionPercentage = entry.Ownership - ((sharesKept / totalSharesAfter) * 100)
                };
            }).ToList();

            // Adicionar novos investidores (compram pro-rata do pool existente)
            foreach (var investor in request.NewInvestors)
            {
                var investorShares = investor.InvestmentAmount / pricePerShare;
                var investorOwnership = (investorShares / totalSharesAfter) * 100;
                
                newInvestors.Add(new SimulatedNewInvestor
                {
                    Name = investor.Name,
                    InvestmentAmount = investor.InvestmentAmount,
                    SharesReceived = investorShares,
                    OwnershipPercentage = investorOwnership,
                    ValueAtRound = investor.InvestmentAmount
                });
                
                capTableAfter.Add(new SimulatedShareholderEntry
                {
                    ShareholderId = null,
                    ShareholderName = investor.Name,
                    ShareholderType = "Investidor",
                    Shares = investorShares,
                    Ownership = investorOwnership,
                    Value = investor.InvestmentAmount,
                    DilutionPercentage = 0,
                    IsNewInvestor = true
                });
            }
        }
        else
        {
            // Primária: diluição aplicada
            capTableAfter = capTableBefore.Select(entry => entry with
            {
                Ownership = (entry.Shares / totalSharesAfter) * 100,
                Value = entry.Shares * pricePerShare,
                DilutionPercentage = entry.Ownership - ((entry.Shares / totalSharesAfter) * 100)
            }).ToList();
            
            // Adicionar novos investidores
            foreach (var investor in request.NewInvestors)
            {
                var investorShares = investor.InvestmentAmount / pricePerShare;
                var investorOwnership = (investorShares / totalSharesAfter) * 100;
                
                newInvestors.Add(new SimulatedNewInvestor
                {
                    Name = investor.Name,
                    InvestmentAmount = investor.InvestmentAmount,
                    SharesReceived = investorShares,
                    OwnershipPercentage = investorOwnership,
                    ValueAtRound = investor.InvestmentAmount
                });
                
                capTableAfter.Add(new SimulatedShareholderEntry
                {
                    ShareholderId = null,
                    ShareholderName = investor.Name,
                    ShareholderType = "Investidor",
                    Shares = investorShares,
                    Ownership = investorOwnership,
                    Value = investor.InvestmentAmount,
                    DilutionPercentage = 0,
                    IsNewInvestor = true
                });
            }

            // Se não há investidores específicos, criar um genérico
            if (request.NewInvestors.Count == 0)
            {
                var totalInvestorShares = newShares;
                var investorOwnership = (totalInvestorShares / totalSharesAfter) * 100;
                
                newInvestors.Add(new SimulatedNewInvestor
                {
                    Name = "Novo Investidor",
                    InvestmentAmount = request.InvestmentAmount,
                    SharesReceived = totalInvestorShares,
                    OwnershipPercentage = investorOwnership,
                    ValueAtRound = request.InvestmentAmount
                });
                
                capTableAfter.Add(new SimulatedShareholderEntry
                {
                    ShareholderId = null,
                    ShareholderName = "Novo Investidor",
                    ShareholderType = "Investidor",
                    Shares = totalInvestorShares,
                    Ownership = investorOwnership,
                    Value = request.InvestmentAmount,
                    DilutionPercentage = 0,
                    IsNewInvestor = true
                });
            }

            // Adicionar pool de opções ao cap table after se aplicável
            if (optionPoolInfo != null)
            {
                var poolOwnership = (optionPoolInfo.Shares / totalSharesAfter) * 100;
                capTableAfter.Add(new SimulatedShareholderEntry
                {
                    ShareholderId = null,
                    ShareholderName = "Pool de Opções",
                    ShareholderType = "ESOP",
                    Shares = optionPoolInfo.Shares,
                    Ownership = poolOwnership,
                    Value = optionPoolInfo.Value,
                    DilutionPercentage = 0,
                    IsNewInvestor = false
                });
            }
        }
        
        // Ordenar cap tables por ownership desc
        capTableBefore = capTableBefore.OrderByDescending(e => e.Ownership).ToList();
        capTableAfter = capTableAfter.OrderByDescending(e => e.Ownership).ToList();

        // ── Vesting (Fully Diluted) ───────────────────────────────────────────
        var vestingEntries = new List<VestingSimulationEntry>();
        var fullyDilutedCapTable = new List<SimulatedShareholderEntry>();
        var fullyDilutedShares = totalSharesAfter;

        if (request.IncludeVesting)
        {
            var grants = await _vestingGrantRepository.GetActiveGrantsForCompanyAsync(clientId, request.CompanyId);
            var now = DateTime.UtcNow;

            // Total de ações não exercitadas dos grants (unvested + vested-not-exercised)
            var vestingSharesTotal = grants.Sum(g => g.TotalShares - g.ExercisedShares);
            fullyDilutedShares = totalSharesAfter + vestingSharesTotal;

            foreach (var grant in grants)
            {
                var remaining = grant.TotalShares - grant.ExercisedShares;
                if (remaining <= 0) continue;

                var fdOwnership = fullyDilutedShares > 0
                    ? (remaining / fullyDilutedShares) * 100
                    : 0;

                vestingEntries.Add(new VestingSimulationEntry
                {
                    GrantId = grant.Id,
                    ShareholderName = grant.ShareholderId.ToString(), // será resolvido abaixo
                    PlanName = grant.VestingPlanId.ToString(),        // será resolvido abaixo
                    TotalShares = grant.TotalShares,
                    VestedShares = grant.VestedShares,
                    UnvestedShares = grant.UnvestedShares,
                    ExercisedShares = grant.ExercisedShares,
                    RemainingShares = remaining,
                    VestedPercentage = grant.CalculateVestedPercentage(now),
                    FullyDilutedOwnership = fdOwnership,
                    VestingEndDate = grant.VestingEndDate,
                    Status = grant.Status.ToString()
                });
            }

            // Resolver nomes dos acionistas nos grants
            var enrichedEntries = new List<VestingSimulationEntry>();
            foreach (var entry in vestingEntries)
            {
                var grant = grants.First(g => g.Id == entry.GrantId);
                var shareholder = await _shareholderRepository.GetByIdAsync(grant.ShareholderId, clientId);
                // Para o nome do plano, usamos o ID (seria necessário resolver via VestingPlanRepository)
                enrichedEntries.Add(entry with
                {
                    ShareholderName = shareholder?.Name ?? "Desconhecido"
                });
            }
            vestingEntries = enrichedEntries;

            // Construir fully diluted cap table (capTableAfter + vesting grants)
            // Ajustar ownership dos holders existentes para base FD
            fullyDilutedCapTable = capTableAfter.Select(e => e with
            {
                Ownership = fullyDilutedShares > 0 ? (e.Shares / fullyDilutedShares) * 100 : 0
            }).ToList();

            // Agregar vesting por acionista
            var vestingByHolder = vestingEntries
                .GroupBy(v => v.ShareholderName)
                .Select(g => new SimulatedShareholderEntry
                {
                    ShareholderId = null,
                    ShareholderName = g.Key,
                    ShareholderType = "Vesting",
                    Shares = g.Sum(v => v.RemainingShares),
                    Ownership = g.Sum(v => v.FullyDilutedOwnership),
                    Value = g.Sum(v => v.RemainingShares) * pricePerShare,
                    DilutionPercentage = 0,
                    IsNewInvestor = false
                });

            // Mesclar com holders existentes (se o acionista já está no cap table)
            var fdDict = fullyDilutedCapTable.ToDictionary(e => e.ShareholderName, e => e);
            foreach (var vestHolder in vestingByHolder)
            {
                if (fdDict.TryGetValue(vestHolder.ShareholderName, out var existing))
                {
                    var mergedShares = existing.Shares + vestHolder.Shares;
                    fdDict[vestHolder.ShareholderName] = existing with
                    {
                        Shares = mergedShares,
                        Ownership = fullyDilutedShares > 0 ? (mergedShares / fullyDilutedShares) * 100 : 0,
                        Value = mergedShares * pricePerShare
                    };
                }
                else
                {
                    fdDict[vestHolder.ShareholderName] = vestHolder;
                }
            }

            fullyDilutedCapTable = fdDict.Values
                .OrderByDescending(e => e.Ownership)
                .ToList();
        }

        return new RoundSimulationResponse
        {
            RoundName = request.RoundName,
            PreMoneyValuation = request.PreMoneyValuation,
            InvestmentAmount = request.InvestmentAmount,
            PostMoneyValuation = postMoneyValuation,
            PricePerShare = pricePerShare,
            SharesBefore = totalSharesBefore,
            NewSharesIssued = newShares + (optionPoolInfo?.Shares ?? 0),
            SharesAfter = totalSharesAfter,
            TotalDilution = totalDilution * 100,
            CapTableBefore = capTableBefore,
            CapTableAfter = capTableAfter,
            NewInvestors = newInvestors,
            OptionPool = optionPoolInfo,
            AcquisitionType = request.AcquisitionType,
            VestingEntries = vestingEntries,
            FullyDilutedCapTable = fullyDilutedCapTable,
            FullyDilutedShares = fullyDilutedShares,
            SimulatedAt = DateTime.UtcNow
        };
    }

    public async Task<decimal> CalculateDilutionAsync(Guid clientId, Guid companyId, decimal investmentAmount, decimal preMoneyValuation)
    {
        if (preMoneyValuation <= 0 || investmentAmount <= 0)
            return 0;
        
        // Verificar se empresa existe
        var company = await _companyRepository.GetByIdAsync(companyId);
        if (company == null || company.ClientId != clientId)
            throw new NotFoundException("Company", companyId);

        // Obter cap table atual
        var currentShares = await _shareRepository.GetActiveByCompanyAsync(clientId, companyId);
        
        var totalSharesBefore = currentShares.Sum(s => s.Quantity);
        if (totalSharesBefore == 0)
            totalSharesBefore = 1_000_000;
        
        var pricePerShare = preMoneyValuation / totalSharesBefore;
        var newShares = investmentAmount / pricePerShare;
        var totalSharesAfter = totalSharesBefore + newShares;
        
        return (1 - (totalSharesBefore / totalSharesAfter)) * 100;
    }

    private async Task<List<SimulatedShareholderEntry>> BuildCapTableEntries(
        IEnumerable<Share> shares, 
        decimal totalShares, 
        decimal pricePerShare)
    {
        var entries = new List<SimulatedShareholderEntry>();
        
        // Agrupar por acionista
        var grouped = shares.GroupBy(s => s.ShareholderId);
        
        foreach (var group in grouped)
        {
            var shareholderShares = group.Sum(s => s.Quantity);
            var shareholderValue = shareholderShares * pricePerShare;
            var ownership = (shareholderShares / totalShares) * 100;
            
            // Pegar o nome do primeiro share (todos têm o mesmo shareholder)
            var firstShare = group.First();
            
            // Buscar tipo do acionista
            var shareholder = await _shareholderRepository.GetByIdAsync(group.Key, firstShare.ClientId);
            var shareholderType = shareholder?.Type switch
            {
                ShareholderType.Founder => "Fundador",
                ShareholderType.Investor => "Investidor",
                ShareholderType.Employee => "Funcionário",
                ShareholderType.Advisor => "Conselheiro",
                ShareholderType.ESOP => "ESOP",
                ShareholderType.Partner => "Sócio",
                _ => "Outro"
            };
            
            entries.Add(new SimulatedShareholderEntry
            {
                ShareholderId = group.Key,
                ShareholderName = shareholder?.Name ?? "Desconhecido",
                ShareholderType = shareholderType,
                Shares = shareholderShares,
                Ownership = ownership,
                Value = shareholderValue,
                DilutionPercentage = 0,
                IsNewInvestor = false
            });
        }
        
        return entries;
    }

    private decimal CalculateOptionPoolShares(
        decimal currentShares, 
        decimal newInvestorShares, 
        decimal poolPercentage, 
        bool isPreMoney)
    {
        if (isPreMoney)
        {
            // Pool pré-money: percentual é calculado sobre o cap table pós-rodada
            // Fórmula: poolShares = (targetPercentage / (1 - targetPercentage)) * (currentShares + newShares)
            var targetPercentage = poolPercentage / 100;
            var totalWithoutPool = currentShares + newInvestorShares;
            return (targetPercentage / (1 - targetPercentage)) * totalWithoutPool;
        }
        else
        {
            // Pool pós-money: percentual é simplesmente sobre o total após a rodada
            var totalAfterRound = currentShares + newInvestorShares;
            return totalAfterRound * (poolPercentage / 100);
        }
    }
}
