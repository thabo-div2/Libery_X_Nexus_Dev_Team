using API.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Realtime;

// Publish only after the action has finished committing. Events contain no records or identifiers;
// every recipient reloads its own data through the existing authorized API endpoints.
public sealed class PublishUpdatesFilter(IHubContext<UpdatesHub> hub,
    IDbContextFactory<ApplicationDbContext> factory, ILogger<PublishUpdatesFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();
        var http = context.HttpContext;
        var status = (result.Result as Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;
        if (result.Exception is not null || status >= 400 || HttpMethods.IsGet(http.Request.Method)
            || HttpMethods.IsHead(http.Request.Method) || http.User.Identity?.IsAuthenticated != true) return;
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            var advisor = await UpdateScope.AdvisorAsync(http.User, db);
            if (advisor is int id) await hub.Clients.Group(UpdateScope.Group(id)).SendAsync("DataChanged");
        }
        catch (Exception ex) { logger.LogWarning(ex, "Realtime update delivery failed after a saved action."); }
    }
}
