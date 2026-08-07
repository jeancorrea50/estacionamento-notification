using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EstacionamentoNotification.API.Hubs;

[Authorize]
public sealed class NotificacaoHub : Hub
{
    public const string HubPath = "/hubs/notificacao";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue("unique_name")
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        if (int.TryParse(userId, out var id) && id > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{id}");

        var roles = Context.User?.FindAll("role")
            .Concat(Context.User?.FindAll(ClaimTypes.Role) ?? Enumerable.Empty<Claim>())
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            ?? Enumerable.Empty<string>();

        if (roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)))
            await Groups.AddToGroupAsync(Context.ConnectionId, "role:Admin");

        await base.OnConnectedAsync();
    }
}
