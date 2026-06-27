using Prodea.Api.Models;

namespace Prodea.Api.Extensions;

public static class UserExtensions
{
    public static string? FullName(this User user) =>
        (user.FirstName != null || user.LastName != null)
            ? $"{user.FirstName} {user.LastName}".Trim()
            : null;
}
