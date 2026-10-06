using EstacionamentoNotification.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EstacionamentoNotification.API.Hubs;

/// <summary>Hub de notificações — somente usuários com role Admin.</summary>
[Authorize(Roles = "Admin")]
public sealed class NotificacaoHub : Hub
{
    public const string HubPath = "/hubs/notificacao";

    public override async Task OnConnectedAsync()
    {
        if (!Context.User.IsInRoleAdmin())
        {
            Context.Abort();
            return;
        }

        var userId = Context.User.ResolveUsuarioId();
        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        await Groups.AddToGroupAsync(Context.ConnectionId, "role:Admin");

        await base.OnConnectedAsync();
    }
}
