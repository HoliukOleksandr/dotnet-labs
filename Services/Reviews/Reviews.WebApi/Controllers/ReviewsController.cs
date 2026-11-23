using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Common.Models;
using Reviews.Application.Reviews;
using Reviews.Application.Reviews.Commands.AddComment;
using Reviews.Application.Reviews.Commands.CreateReview;
using Reviews.Application.Reviews.Commands.DeleteReview;
using Reviews.Application.Reviews.Commands.UpdateReview;
using Reviews.Application.Reviews.Queries.GetReviewById;
using Reviews.Application.Reviews.Queries.GetReviewsCursor;
using Reviews.Application.Reviews.Queries.GetReviewsList;

namespace Reviews.WebApi.Controllers;

public record UpdateReviewRequest(int Rating, string? Text);
public record AddCommentRequest(string AuthorId, string AuthorName, string Text);

[Route("api/reviews")]
public class ReviewsController : ApiController
{
    /// <summary>Список з пагінацією (skip/limit у MongoDB), фільтром за товаром і повнотекстовим пошуком.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<ReviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetList(
        [FromQuery] long? productId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetReviewsListQuery(productId, search, page, pageSize), ct));

    /// <summary>Курсорна пагінація: передайте nextCursor з попередньої відповіді як параметр after.</summary>
    [HttpGet("cursor")]
    [ProducesResponseType<CursorPage<ReviewDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CursorPage<ReviewDto>>> GetCursor(
        [FromQuery] long productId, [FromQuery] string? after,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetReviewsCursorQuery(productId, after, pageSize), ct));

    /// <summary>Відгук за ідентифікатором; версія документа повертається в заголовку ETag.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken ct)
    {
        var review = await Mediator.Send(new GetReviewByIdQuery(id), ct);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    /// <summary>Створити відгук; другий відгук того самого автора на той самий товар -> 400/409.</summary>
    [HttpPost]
    [ProducesResponseType<string>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> Create(CreateReviewCommand command, CancellationToken ct)
    {
        var id = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Змінити відгук. Заголовок If-Match (версія з ETag) — необов'язковий; якщо вказаний і застарів -> 409.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Update(string id, UpdateReviewRequest body, CancellationToken ct)
    {
        if (!TryReadIfMatch(out var expectedVersion))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "If-Match must contain the document version, e.g. \"3\"");

        var review = await Mediator.Send(new UpdateReviewCommand(id, body.Rating, body.Text, expectedVersion), ct);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    [HttpPost("{id}/comments")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> AddComment(string id, AddCommentRequest body, CancellationToken ct)
    {
        if (!TryReadIfMatch(out var expectedVersion))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "If-Match must contain the document version, e.g. \"3\"");

        var review = await Mediator.Send(
            new AddCommentCommand(id, body.AuthorId, body.AuthorName, body.Text, expectedVersion), ct);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteReviewCommand(id), ct);
        return NoContent();
    }

    private bool TryReadIfMatch(out int? version)
    {
        version = null;
        var header = Request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(header)) return true;
        if (!int.TryParse(header.Trim().Trim('"'), out var parsed)) return false;
        version = parsed;
        return true;
    }
}
