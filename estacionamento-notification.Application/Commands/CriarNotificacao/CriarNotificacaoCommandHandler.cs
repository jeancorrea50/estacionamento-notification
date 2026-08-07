using EstacionamentoNotification.Application.Abstractions;
using EstacionamentoNotification.Domain.Entities;
using EstacionamentoNotification.Domain.Interfaces;
using MediatR;

namespace EstacionamentoNotification.Application.Commands.CriarNotificacao;

public sealed class CriarNotificacaoCommandHandler
    : IRequestHandler<CriarNotificacaoCommand, CriarNotificacaoResult>
{
    public const string AdminRoleName = "Admin";

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

        var usuarioIds = new HashSet<int>(request.UsuarioIds ?? Enumerable.Empty<int>());

        if (request.NotificarRoleAdmin)
        {
            var admins = await _repository.ListUsuarioIdsByRoleAsync(AdminRoleName, cancellationToken);
            foreach (var id in admins)
                usuarioIds.Add(id);
        }

        var entity = new Notificacao
        {
            Tipo = request.Tipo.Trim(),
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

        if (!string.IsNullOrWhiteSpace(request.CodExportacao))
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
            CodExportacao = request.CodExportacao
        };

        await _publisher.PublishAsync(payload, usuarioIds, cancellationToken);

        return new CriarNotificacaoResult
        {
            Id = saved.Id,
            SignalREnviado = true
        };
    }
}
