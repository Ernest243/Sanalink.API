namespace Sanalink.API.Services;

public interface IRoleSeeder
{
    Task SeedRolesAsync();
    Task SeedAdminUserAsync();
}