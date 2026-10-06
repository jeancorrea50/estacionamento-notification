using EstacionamentoNotification.Application.Abstractions;
using EstacionamentoNotification.Domain.Entities;
using EstacionamentoNotification.Domain.Interfaces;
using MediatR;

namespace EstacionamentoNotification.Application.Commands.CriarNotificacao;

public sealed class CriarNotificacaoCommandHandler
    : IRequestHandler<CriarNotificacaoCommand, CriarNotificacaoResult>
{
    public const string AdminRoleName = "Admin";

    /// <summary>Tipos operacionais de infraestrutura — somente perfil Admin.</summary>
    private static readonly HashSet<string> TiposSomenteAdmin = new(StringComparer.OrdinalIgnoreCase)
    {
        "GtsMigracao",
        "ExcluirBanco"
    };

    private readonly INotificacaoRepository _repository;
    private readonly INotificacaoRealtimePublisher _publisher;

    public CriarNotificacaoCommandHandler(
        INotificacaoRepository repository,
        INotificacaoRealtimePublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<CriarNotificacaoResult> Handle(
        CriarNotificacaoCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Tipo))
            throw new ArgumentException("Tipo é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Titulo))
            throw new ArgumentException("Titulo é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Mensagem))
            throw new ArgumentException("Mensagem é obrigatória.");

        var tipo = request.Tipo.Trim();
        var somenteAdmin = TiposSomenteAdmin.Contains(tipo) || request.NotificarRoleAdmin;

        var adminIds = await _repository.ListUsuarioIdsByRoleAsync(AdminRoleName, cancellationToken);
        var adminSet = new HashSet<int>(adminIds);

        var usuarioIds = new HashSet<int>();

        if (somenteAdmin)
        {
            // Migration / exclusão de banco (e qualquer NotificarRoleAdmin):
            // destina APENAS usuários com role Admin — ignora UsuarioIds extras.
            foreach (var id in adminSet)
                usuarioIds.Add(id);
        }
        else
        {
            foreach (var id in request.UsuarioIds ?? Enumerable.Empty<int>())
            {
                if (id > 0)
                    usuarioIds.Add(id);
            }
        }

        if (usuarioIds.Count == 0 && somenteAdmin)
        {
            // Sem Admin no Identity: não cria notificação “órfã” nem faz broadcast amplo.
            return new CriarNotificacaoResult { Id = 0, SignalREnviado = false };
        }

        var entity = new Notificacao
        {
            Tipo = tipo,
            Titulo = request.Titulo.Trim(),
            Mensagem = request.Mensagem.Trim(),
            DadosJson = request.DadosJson,
            ReferenciaTipo = request.ReferenciaTipo,
            ReferenciaId = request.ReferenciaId,
            Usuarios = usuarioIds.Select(uid => new NotificacaoUsuario
            {
                UsuarioId = uid,
                Lida = false
            }).ToList()
        };

        // Escopo por pátio não se aplica a notificações só-Admin (evita vazamento por CodExportacao).
        if (!somenteAdmin && !string.IsNullOrWhiteSpace(request.CodExportacao))
        {
            entity.Estacionamentos.Add(new NotificacaoEstacionamento
            {
                CodExportacao = request.CodExportacao.Trim()
            });
        }

        var saved = await _repository.AddAsync(entity, cancellationToken);

        var payload = new
        {
            saved.Id,
            saved.Tipo,
            saved.Titulo,
            saved.Mensagem,
            saved.DadosJson,
            saved.ReferenciaTipo,
            saved.ReferenciaId,
            saved.DataCriacao,
            CodExportacao = somenteAdmin ? null : request.CodExportacao
        };

        await _publisher.PublishAsync(
            payload,
            usuarioIds,
            cancellationToken,
            notificarRoleAdmin: somenteAdmin);

        return new CriarNotificacaoResult
        {
            Id = saved.Id,
            SignalREnviado = true
        };
    }
}
