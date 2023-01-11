using OpenMod.API.Ioc;

namespace Economy.API;

[Service]
public interface IAccountsRepository
{
    Task<Account?> GetAsync(string ownerId, string ownerType);
    Task UpsertAsync(Account account);
}