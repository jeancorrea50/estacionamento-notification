using EstacionamentoNotification.Domain.Entities;

namespace EstacionamentoNotification.Domain.Interfaces;

public interface INotificacaoRepository
{
    Task<Notificacao> AddAsync(Notificacao entity, CancellationToken cancellationToken = default);
    Task<Notificacao?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notificacao>> ListByUsuarioAsync(int usuarioId, int take, CancellationToken cancellationToken = default);
    Task MarkReadAsync(long notificacaoId, int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> ListUsuarioIdsByRoleAsync(string roleName, CancellationToken cancellationToken = default);
}
