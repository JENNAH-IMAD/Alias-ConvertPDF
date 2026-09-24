namespace BankStatementConverter.Application;

public record DashboardDto(int Clients, int Banks, int Accounts);
public record DeletionPreview(string Name, int Accounts, string Version);
public record ImageDto(string? DataUrl);

public interface ICatalogService<TInput, TDto>
{
    Task<PageResult<TDto>> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<TDto> GetAsync(Guid id, CancellationToken ct);
    Task<TDto> SaveAsync(Guid? id, TInput input, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IBankService
{
    Task<PageResult<BankDto>> ListAsync(int page, int pageSize, string? search, string sort, CancellationToken ct);
    Task<BankDto> GetAsync(Guid id, CancellationToken ct);
    Task<BankDto> SaveAsync(Guid? id, BankInput input, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<PageResult<BankAccountSummary>> AccountsAsync(Guid id, int page, int pageSize, CancellationToken ct);
}

public interface ICatalogDeletionService
{
    Task<DeletionPreview> PreviewAsync(string resource, Guid id, CancellationToken ct);
    Task DeleteAsync(string resource, Guid id, string version, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct);
}

public interface ICatalogImageService
{
    Task<ImageDto> GetAsync(bool bank, Guid id, CancellationToken ct);
    Task<Guid> SaveAsync(bool bank, Guid id, Stream input, long length, CancellationToken ct);
    Task DeleteAsync(bool bank, Guid id, CancellationToken ct);
}
