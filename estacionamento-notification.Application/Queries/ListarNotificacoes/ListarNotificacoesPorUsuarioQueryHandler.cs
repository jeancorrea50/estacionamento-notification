using EstacionamentoNotification.Domain.Interfaces;
using MediatR;

namespace EstacionamentoNotification.Application.Queries.ListarNotificacoes;

public sealed class ListarNotificacoesPorUsuarioQueryHandler
    : IRequestHandler<ListarNotificacoesPorUsuarioQuery, IReadOnlyList<NotificacaoDto>>
{
    private readonly INotificacaoRepository _repository;

    public ListarNotificacoesPorUsuarioQueryHandler(INotificacaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<NotificacaoDto>> Handle(
        ListarNotificacoesPorUsuarioQuery request,
        CancellationToken cancellationToken)
    {
        var lista = await _repository.ListByUsuarioAsync(request.UsuarioId, request.Take, cancellationToken);

        return lista.Select(n =>
        {
            var vinculo = n.Usuarios.FirstOrDefault(u => u.UsuarioId == request.UsuarioId);
            return new NotificacaoDto
            {
                Id = n.Id,
                Tipo = n.Tipo,
                Titulo = n.Titulo,
                Mensagem = n.Mensagem,
                DadosJson = n.DadosJson,
                DataCriacao = n.DataCriacao,
                Lida = vinculo?.Lida ?? false,
                CodExportacao = n.Estacionamentos.FirstOrDefault()?.CodExportacao
            };
        }).ToList();
    }
}
