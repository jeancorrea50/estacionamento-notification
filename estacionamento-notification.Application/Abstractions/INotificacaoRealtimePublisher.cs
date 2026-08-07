namespace EstacionamentoNotification.Application.Abstractions;

public interface INotificacaoRealtimePublisher
{
    Task PublishAsync(object payload, IEnumerable<int> usuarioIds, CancellationToken cancellationToken = default);
}
