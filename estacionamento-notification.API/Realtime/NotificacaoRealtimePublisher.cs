using EstacionamentoNotification.Application.Abstractions;
using EstacionamentoNotification.API.Hubs;
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
        CancellationToken cancellationToken = default)
    {
        await _hub.Clients.Group(AdminGroup).SendAsync(EventName, payload, cancellationToken);

        foreach (var usuarioId in usuarioIds.Distinct())
        {
            await _hub.Clients.Group($"user:{usuarioId}")
                .SendAsync(EventName, payload, cancellationToken);
        }
    }
}
