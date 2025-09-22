
namespace SurveyBasket.Services
{
    public class VoteService(ApplicationDbContext context) : IVoteService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Result> AddAsync(int pollId, string userId, VoteRequest request, CancellationToken cancellationToken = default)
        {
            var hasVote = await _context.Votes.AnyAsync(v => v.PollId == pollId && v.UserId == userId, cancellationToken);
            if (hasVote) return Result.Failure(VoteErrors.DuplicatedVote);

            var pollIsExists = await _context.Polls.AnyAsync(
                p => p.Id == pollId
                && p.IsPublished
                && p.StartsAt <= DateOnly.FromDateTime(DateTime.Now) && p.EndsAt >= DateOnly.FromDateTime(DateTime.Now)
                , cancellationToken);
            if (!pollIsExists) return Result.Failure(PollErrors.PollNotFound);

            var availableQuestions = await _context.Questions
                .Where(q => q.PollId == pollId && q.IsActive)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            if (!request.Answers.Select(
                q => q.QuestionId).SequenceEqual(availableQuestions)) return Result.Failure(VoteErrors.InvalidQuestions);

            var vote = new Vote
            {
                PollId = pollId,
                UserId = userId,
                VoteAnswers = request.Answers.Adapt<IEnumerable<VoteAnswer>>().ToList(),
            };

            await _context.Votes.AddAsync(vote, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
                }
    }
}
