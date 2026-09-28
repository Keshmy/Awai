using Awai.Models.Entities;
using Awai.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Awai.Controllers
{
    [Authorize(Roles = "Prog,Admin")]
    [ApiController]
    [Route("Payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentFlow _flow;

        public PaymentsController(IPaymentFlow flow)
        {
            _flow = flow;
        }

        [HttpPost("Begin")]
        public async Task<IActionResult> Begin([FromBody] BeginPaymentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _flow.BeginAsync(request, cancellationToken);
                return Ok(ToDto(payment));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Status(Guid id, CancellationToken cancellationToken)
        {
            var payment = await _flow.FindAsync(id, cancellationToken);
            return payment == null ? NotFound() : Ok(ToDto(payment));
        }

        private static object ToDto(PaymentIntent payment) => new
        {
            payment.Id,
            payment.Reference,
            payment.Amount,
            payment.Currency,
            status = payment.Status.ToString(),
            statusText = payment.Status.Arabic(),
            payment.AttemptCount,
            payment.ProviderReference,
            payment.LastError
        };
    }
}
