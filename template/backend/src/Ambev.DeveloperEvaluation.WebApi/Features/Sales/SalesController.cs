using Ambev.DeveloperEvaluation.Application.Sales;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

[ApiController]
[Route("api/sales")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SaleResult), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] SaleInput input,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateSaleCommand(input), cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetSaleQuery(id), cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(SalePage), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ListSalesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new ListSalesQuery(request.ToFilter()), cancellationToken));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SaleInput input,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new UpdateSaleCommand(id, input), cancellationToken));
    }

    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(SaleResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new CancelSaleCommand(id), cancellationToken));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSaleCommand(id), cancellationToken);
        return NoContent();
    }
}
