using EstacionamentoNotification.Domain.Entities;
using MediatR;

namespace EstacionamentoNotification.Application.Queries.ListarNotificacoes;

public sealed class ListarNotificacoesPorUsuarioQuery : IRequest<IReadOnlyList<NotificacaoDto>>
{
    public int UsuarioId { get; set; }
    public int Take { get; set; } = 50;
}

public sealed class NotificacaoDto
{
    public long Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? DadosJson { get; set; }
    public DateTime DataCriacao { get; set; }
    public bool Lida { get; set; }
    public string? CodExportacao { get; set; }
}
