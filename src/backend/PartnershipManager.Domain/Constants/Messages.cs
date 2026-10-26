namespace PartnershipManager.Domain.Constants;

/// <summary>
/// Mensagens de erro do sistema
/// </summary>
public static class ErrorMessages
{
    // =============== VALIDAÇÃO GERAL ===============
    public const string Required = "O campo {0} é obrigatório.";
    public const string InvalidFormat = "O formato do campo {0} é inválido.";
    public const string MaxLength = "O campo {0} deve ter no máximo {1} caracteres.";
    public const string MinLength = "O campo {0} deve ter no mínimo {1} caracteres.";
    public const string Range = "O campo {0} deve estar entre {1} e {2}.";
    public const string InvalidEmail = "O email informado é inválido.";
    public const string InvalidUrl = "A URL informada é inválida.";
    
    // =============== DOCUMENTOS ===============
    public const string InvalidCpf = "O CPF informado é inválido.";
    public const string InvalidCnpj = "O CNPJ informado é inválido.";
    public const string CnpjRequired = "O CNPJ é obrigatório.";
    public const string CnpjAlreadyExists = "Já existe uma empresa cadastrada com este CNPJ.";
    
    // =============== AUTENTICAÇÃO ===============
    public const string InvalidCredentials = "Email ou senha inválidos.";
    public const string UserNotFound = "Usuário não encontrado.";
    public const string UserInactive = "Usuário inativo. Entre em contato com o administrador.";
    public const string UserBlocked = "Usuário bloqueado. Tente novamente em {0} minutos.";
    public const string UserPending = "Usuário pendente de ativação.";
    public const string InvalidToken = "Token inválido ou expirado.";
    public const string RefreshTokenExpired = "Sessão expirada. Faça login novamente.";
    public const string Unauthorized = "Acesso não autorizado.";
    public const string Forbidden = "Você não tem permissão para realizar esta ação.";
    
    // =============== SENHA ===============
    public const string PasswordRequired = "A senha é obrigatória.";
    public const string PasswordMinLength = "A senha deve ter no mínimo {0} caracteres.";
    public const string PasswordMaxLength = "A senha deve ter no máximo {0} caracteres.";
    public const string PasswordUppercase = "A senha deve conter pelo menos uma letra maiúscula.";
    public const string PasswordLowercase = "A senha deve conter pelo menos uma letra minúscula.";
    public const string PasswordNumber = "A senha deve conter pelo menos um número.";
    public const string PasswordSpecial = "A senha deve conter pelo menos um caractere especial.";
    public const string PasswordMismatch = "As senhas não conferem.";
    public const string PasswordSameAsCurrent = "A nova senha não pode ser igual à senha atual.";
    public const string CurrentPasswordInvalid = "A senha atual está incorreta.";
    
    // =============== EMPRESA ===============
    public const string CompanyNotFound = "Empresa não encontrada.";
    public const string CompanyInactive = "Empresa inativa.";
    public const string CompanyNameRequired = "O nome da empresa é obrigatório.";
    public const string InvalidFoundationDate = "A data de fundação não pode ser futura.";
    public const string InvalidTotalShares = "O total de ações deve ser maior que zero.";
    public const string InvalidSharePrice = "O preço por ação deve ser maior que zero.";
    public const string InvalidLegalForm = "Forma jurídica inválida.";
    public const string InvalidCurrency = "Moeda inválida.";
    
    // =============== USUÁRIO ===============
    public const string EmailAlreadyExists = "Já existe um usuário com este email nesta empresa.";
    public const string UserAlreadyExists = "Já existe um usuário com este email.";
    public const string EmailRequired = "O email é obrigatório.";
    public const string NameRequired = "O nome é obrigatório.";
    public const string InvalidRole = "Papel inválido.";
    public const string UserAlreadyHasRole = "O usuário já possui este papel.";
    public const string CannotRemoveLastAdmin = "Não é possível remover o último administrador.";
    public const string CannotDeactivateSelf = "Você não pode desativar sua própria conta.";
    public const string CannotDeactivateYourself = "Você não pode desativar sua própria conta.";
    
    // =============== SISTEMA ===============
    public const string NotFound = "{0} não encontrado(a).";
    public const string AlreadyExists = "{0} já existe.";
    public const string InternalError = "Ocorreu um erro interno. Tente novamente mais tarde.";
    public const string DatabaseError = "Erro ao acessar o banco de dados.";
}

/// <summary>
/// Mensagens de sucesso do sistema
/// </summary>
public static class SuccessMessages
{
    // =============== AUTENTICAÇÃO ===============
    public const string LoginSuccess = "Login realizado com sucesso.";
    public const string LogoutSuccess = "Logout realizado com sucesso.";
    public const string PasswordChanged = "Senha alterada com sucesso.";
    public const string PasswordResetSent = "Email de recuperação enviado com sucesso.";
    
    // =============== EMPRESA ===============
    public const string CompanyCreated = "Empresa criada com sucesso.";
    public const string CompanyUpdated = "Empresa atualizada com sucesso.";
    public const string CompanyDeleted = "Empresa excluída com sucesso.";
    public const string CompanyActivated = "Empresa ativada com sucesso.";
    public const string CompanyDeactivated = "Empresa desativada com sucesso.";
    
    // =============== USUÁRIO ===============
    public const string UserCreated = "Usuário criado com sucesso.";
    public const string UserUpdated = "Usuário atualizado com sucesso.";
    public const string UserDeleted = "Usuário excluído com sucesso.";
    public const string UserActivated = "Usuário ativado com sucesso.";
    public const string UserDeactivated = "Usuário desativado com sucesso.";
    public const string RoleAdded = "Papel adicionado com sucesso.";
    public const string RoleRemoved = "Papel removido com sucesso.";
    
    // =============== GERAL ===============
    public const string Created = "{0} criado(a) com sucesso.";
    public const string Updated = "{0} atualizado(a) com sucesso.";
    public const string Deleted = "{0} excluído(a) com sucesso.";
    public const string Saved = "Dados salvos com sucesso.";
}

/// <summary>
/// Constantes de configuração do sistema
/// </summary>
public static class SystemConstants
{
    // =============== PAGINAÇÃO ===============
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
    
    // =============== CACHE ===============
    public const int CacheExpirationMinutes = 30;
    public const string CachePrefix = "pm:";
    
    // =============== SEGURANÇA ===============
    public const int MaxLoginAttempts = 5;
    public const int LockoutMinutes = 15;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 100;
    public const int TokenExpirationHours = 24;
    public const int RefreshTokenDays = 7;
    public const int SessionIdleTimeoutMinutes = 5;
    
    // =============== VALIDAÇÃO ===============
    public const int MaxNameLength = 200;
    public const int MaxEmailLength = 255;
    public const int MaxUrlLength = 500;
    public const int CnpjLength = 14;
    public const int CpfLength = 11;
}

/// <summary>
/// Nomes das tabelas do banco de dados
/// </summary>
public static class TableNames
{
    public const string Companies = "companies";
    public const string Users = "users";
    public const string UserRoles = "user_roles";
    public const string AuditLogs = "audit_logs";
    public const string Shareholders = "shareholders";
    public const string Shares = "shares";
    public const string ShareTransactions = "share_transactions";
    public const string Contracts = "contracts";
    public const string VestingPlans = "vesting_plans";
    public const string VestingGrants = "vesting_grants";
    public const string VestingSchedules = "vesting_schedules";
    public const string VestingMilestones = "vesting_milestones";
    public const string VestingTransactions = "vesting_transactions";
    public const string Valuations = "valuations";
    public const string Workflows = "workflows";
}

/// <summary>
/// Chaves de cache
/// </summary>
public static class CacheKeys
{
    private const string Prefix = SystemConstants.CachePrefix;
    
    public static string Company(Guid id) => $"{Prefix}company:{id}";
    public static string CompanyByCnpj(string cnpj) => $"{Prefix}company:cnpj:{cnpj}";
    public static string User(Guid id) => $"{Prefix}user:{id}";
    public static string UserByEmail(string email, Guid companyId) => $"{Prefix}user:{companyId}:{email}";
    public static string UserRoles(Guid userId) => $"{Prefix}roles:{userId}";
}
