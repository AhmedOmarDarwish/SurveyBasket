﻿using Microsoft.AspNetCore.RateLimiting;

namespace SurveyBasket.Controllers
{
    [Route("api/polls/{pollId}/vote")]
    [ApiController]
    [Authorize(Roles = DefaultRoles.Member)]
    [EnableRateLimiting(RateLimiters.Concurrency)]
    public class VotesController(IQuestionService questionService, IVoteService voteService) : ControllerBase
    {
        private readonly IQuestionService _questionService = questionService;
        private readonly IVoteService _voteService = voteService;

        [HttpGet("")]
        public async Task<IActionResult> Start([FromRoute] int pollId, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId()!;
            var result = await _questionService.GetAvailableAsync(pollId, userId, cancellationToken);
            return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
        }

        [HttpPost("")]
        public async Task<IActionResult> Vote([FromRoute] int pollId, [FromBody] VoteRequest request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId()!;
            var result = await _voteService.AddAsync(pollId, userId, request ,cancellationToken);
            return result.IsSuccess ? Created() : result.ToProblem();

        }

    }
}
 