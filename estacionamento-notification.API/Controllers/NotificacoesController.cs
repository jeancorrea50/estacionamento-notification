using System.Security.Claims;
using EstacionamentoNotification.Application.Queries.ListarNotificacoes;
using EstacionamentoNotification.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstacionamentoNotification.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/notificacoes")]
public sealed class NotificacoesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly INotificacaoRepository _repository;

    public NotificacoesController(IMediator mediator, INotificacaoRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificacaoDto>>> Listar(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ResolveUsuarioId();
        if (usuarioId <= 0)
            return Unauthorized();

        var lista = await _mediator.Send(new ListarNotificacoesPorUsuarioQuery
        {
            UsuarioId = usuarioId,
            Take = take
        }, cancellationToken);

        return Ok(lista);
    }

    [HttpPost("{id:long}/lida")]
    public async Task<ActionResult> MarcarLida(long id, CancellationToken cancellationToken)
    {
        var usuarioId = ResolveUsuarioId();
        if (usuarioId <= 0)
            return Unauthorized();

        await _repository.MarkReadAsync(id, usuarioId, cancellationToken);
        return Ok(new { sucesso = true });
    }

    private int ResolveUsuarioId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("unique_name")
            ?? User.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : 0;
    }
}
