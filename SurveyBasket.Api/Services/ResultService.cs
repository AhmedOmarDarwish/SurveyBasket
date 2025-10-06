namespace SurveyBasket.Services
{
    public class ResultService(ApplicationDbContext context) : IResultService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Result<PollVotesResponse>> GetPollVotesAsync(int pollId, CancellationToken cancellationToken = default)
        {
            var pollVotes = await _context.Polls
                .Where(p => p.Id == pollId)
                .Select(p => new PollVotesResponse
                (
                    p.Title,
                    p.Votes.Select(v => new VoteResponse
                    (
                        v.User.GetUserFullName()!,
                        v.SubmittedOn,
                        v.VoteAnswers.Select(a => new QuestionAnswerResponse
                        (
                            a.Question.Content,
                            a.Answer.Content
                        ))
                    ))
                )).SingleOrDefaultAsync(cancellationToken);
            return pollVotes is null ? Result.Failure<PollVotesResponse>(PollErrors.PollNotFound) : Result.Success(pollVotes);
        }

        public async Task<Result<IEnumerable<VotesPerDayResponse>>> GetVotesPerDayAsync(int pollId, CancellationToken cancellationToken = default)
        {
            var pollIsExists = await _context.Polls.AnyAsync(p => p.Id == pollId, cancellationToken);
            if (!pollIsExists) return Result.Failure<IEnumerable<VotesPerDayResponse>>(PollErrors.PollNotFound);

            var votesPerDay = await _context.Votes
                .Where(v => v.PollId == pollId)
                .GroupBy(vote => new
                {
                    Date = DateOnly.FromDateTime(vote.SubmittedOn)
                })
                .Select(g => new VotesPerDayResponse(
                    g.Key.Date,
                    g.Count()
                ))
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<VotesPerDayResponse>>(votesPerDay);
        }

        public async Task<Result<IEnumerable<VotesPerQuestionsResponse>>> GetVotesPerQuestionAsync(int pollId, CancellationToken cancellationToken = default)
        {
            var pollIsExists = await _context.Polls.AnyAsync(p => p.Id == pollId, cancellationToken);
            if (!pollIsExists) return Result.Failure<IEnumerable<VotesPerQuestionsResponse>>(PollErrors.PollNotFound);

            var votePerQuestion = await _context.VoteAnswers
                .Where(x => x.Vote.PollId == pollId)
                .Select(x => new VotesPerQuestionsResponse(
                       x.Question.Content,
                       x.Question.Votes.GroupBy(x => new { AnswersId = x.Answer.Id, AnswerContent = x.Answer.Content })
                       .Select(g => new VotesPerAnswerResponse(
                           g.Key.AnswerContent,
                           g.Count()
                           ))
                )).ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<VotesPerQuestionsResponse>>(votePerQuestion);
        }
    }
}
