using EstacionamentoNotification.Application.Commands.CriarNotificacao;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EstacionamentoNotification.API.Controllers.Internal;

[ApiController]
[AllowAnonymous]
[Route("api/internal/notificacoes")]
public sealed class NotificacoesInternalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public NotificacoesInternalController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<ActionResult<CriarNotificacaoResult>> Criar(
        [FromBody] CriarNotificacaoCommand command,
        CancellationToken cancellationToken)
    {
        if (!IsInternalAuthorized())
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    private bool IsInternalAuthorized()
    {
        var expected = _configuration["Notification:InternalKey"];
        if (string.IsNullOrWhiteSpace(expected))
            return false;

        if (!Request.Headers.TryGetValue("X-Internal-Key", out var key))
            return false;

        return string.Equals(key.ToString(), expected, StringComparison.Ordinal);
    }
}
