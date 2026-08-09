using EstacionamentoNotification.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EstacionamentoNotification.API.Hubs;

[Authorize]
public sealed class NotificacaoHub : Hub
{
    public const string HubPath = "/hubs/notificacao";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User.ResolveUsuarioId();
        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        if (Context.User.IsInRoleAdmin())
            await Groups.AddToGroupAsync(Context.ConnectionId, "role:Admin");

        await base.OnConnectedAsync();
    }
}
