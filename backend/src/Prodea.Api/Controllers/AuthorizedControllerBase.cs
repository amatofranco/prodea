using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Prodea.Api.Controllers;

public abstract class AuthorizedControllerBase : ControllerBase
{
    protected int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
