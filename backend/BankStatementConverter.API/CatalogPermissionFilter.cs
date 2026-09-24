using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BankStatementConverter.API;

// Applied to every catalog endpoint, including images and dependent deletions.
public class CatalogPermissionFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true || user.IsInRole("Admin")) return;
        var controller = context.RouteData.Values["controller"]?.ToString();
        var module = controller switch {
            "Clients" or "ClientPhoto" => "clients", "Banks" or "BankLogo" => "banks",
            "Accounts" => "accounts", "Dashboard" => "dashboard",
            "CatalogDeletion" => context.RouteData.Values["resource"]?.ToString(), _ => null
        };
        if (module is null) return;
        var method = context.HttpContext.Request.Method;
        var operation = controller == "CatalogDeletion" || method == "DELETE" && controller is not ("ClientPhoto" or "BankLogo") ? "delete" : method == "GET" ? "read" : "write";
        var allowed = user.HasClaim("permission", module + "." + operation);
        if (controller == "CatalogDeletion") allowed &= user.HasClaim("permission", "accounts.delete");
        if (controller == "Banks" && context.RouteData.Values["action"]?.ToString() == "Accounts") allowed &= user.HasClaim("permission", "accounts.read");
        if (!allowed) context.Result = new ForbidResult();
    }
}
