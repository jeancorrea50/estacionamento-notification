using EstacionamentoNotification.API.Hubs;
using EstacionamentoNotification.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace EstacionamentoNotification.API.Realtime;

public sealed class NotificacaoRealtimePublisher : INotificacaoRealtimePublisher
{
    public const string AdminGroup = "role:Admin";
    public const string EventName = "notificacaoRecebida";

    private readonly IHubContext<NotificacaoHub> _hub;

    public NotificacaoRealtimePublisher(IHubContext<NotificacaoHub> hub)
    {
        _hub = hub;
    }

    public async Task PublishAsync(
        object payload,
        IEnumerable<int> usuarioIds,
        CancellationToken cancellationToken = default,
        bool notificarRoleAdmin = true)
    {
        // Admin-only: publica apenas no grupo Admin (evita user:{id} de não-admins).
        if (notificarRoleAdmin)
        {
            await _hub.Clients.Group(AdminGroup).SendAsync(EventName, payload, cancellationToken);
            return;
        }

        foreach (var usuarioId in usuarioIds.Distinct())
        {
            if (usuarioId <= 0) continue;
            await _hub.Clients.Group($"user:{usuarioId}")
                .SendAsync(EventName, payload, cancellationToken);
        }
    }
}
