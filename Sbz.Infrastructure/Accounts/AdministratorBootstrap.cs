using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Sbz.Application.Accounts;
using Sbz.Infrastructure.Persistence;

namespace Sbz.Infrastructure.Accounts;

internal sealed class AdministratorBootstrap(
    ApplicationDbContext db, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles, IConfiguration configuration)
{
    public async Task InitializeAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (!await roles.RoleExistsAsync(AccountRoles.Administrator))
            Check(await roles.CreateAsync(new IdentityRole(AccountRoles.Administrator)));

        var name = configuration["AdminBootstrap:UserName"];
        var password = configuration["AdminBootstrap:Password"];
        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrEmpty(password))
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(password))
                throw new InvalidOperationException("Configure both AdminBootstrap:UserName and AdminBootstrap:Password.");
            if (name.Length > 256 || password.Length > 128)
                throw new InvalidOperationException("The bootstrap username or password is too long.");

            var user = await users.FindByNameAsync(name);
            if (user is null)
            {
                user = new IdentityUser(name);
                Check(await users.CreateAsync(user, password));
                Check(await users.AddToRoleAsync(user, AccountRoles.Administrator));
            }
            else if (!await users.IsInRoleAsync(user, AccountRoles.Administrator))
            {
                throw new InvalidOperationException("The bootstrap username belongs to an existing non-administrator.");
            }
        }

        await transaction.CommitAsync();
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not provision the administrator. {string.Join(", ", result.Errors.Select(error => error.Code))}");
    }
}
