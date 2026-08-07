using MediatR;

namespace EstacionamentoNotification.Application.Commands.CriarNotificacao;

public sealed class CriarNotificacaoCommand : IRequest<CriarNotificacaoResult>
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? DadosJson { get; set; }
    public string? ReferenciaTipo { get; set; }
    public string? ReferenciaId { get; set; }
    public string? CodExportacao { get; set; }
    public bool NotificarRoleAdmin { get; set; } = true;
    public List<int>? UsuarioIds { get; set; }
}

public sealed class CriarNotificacaoResult
{
    public long Id { get; set; }
    public bool SignalREnviado { get; set; }
}
