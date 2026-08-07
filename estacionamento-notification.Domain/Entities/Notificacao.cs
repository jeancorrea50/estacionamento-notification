namespace EstacionamentoNotification.Domain.Entities;

public sealed class Notificacao
{
    public long Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? DadosJson { get; set; }
    public string? ReferenciaTipo { get; set; }
    public string? ReferenciaId { get; set; }
    public DateTime DataCriacao { get; set; }

    public List<NotificacaoUsuario> Usuarios { get; set; } = new();
    public List<NotificacaoEstacionamento> Estacionamentos { get; set; } = new();
}

public sealed class NotificacaoUsuario
{
    public long Id { get; set; }
    public long NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
    public int UsuarioId { get; set; }
    public bool Lida { get; set; }
    public DateTime? DataLeitura { get; set; }
}

public sealed class NotificacaoEstacionamento
{
    public long Id { get; set; }
    public long NotificacaoId { get; set; }
    public Notificacao? Notificacao { get; set; }
    public string CodExportacao { get; set; } = string.Empty;
}
