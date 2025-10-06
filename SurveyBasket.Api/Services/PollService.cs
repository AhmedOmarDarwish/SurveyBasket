using Azure.Core;
using SurveyBasket.Entities;

namespace SurveyBasket.Services
{
    public class PollService(ApplicationDbContext context, INotificationService notificationService1) : IPollService
    {
        private readonly ApplicationDbContext _context  = context;
        private readonly INotificationService _notificationService1 = notificationService1;

        public async Task<IEnumerable<PollResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Polls
            .AsNoTracking()
            .ProjectToType<PollResponse>()
            .ToListAsync(cancellationToken);

        public async Task<IEnumerable<PollResponse>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            await _context.Polls
            .AsNoTracking()
            .Where(p => p.IsPublished && p.StartsAt <= DateOnly.FromDateTime(DateTime.Now) && p.EndsAt >= DateOnly.FromDateTime(DateTime.Now))
            .ProjectToType<PollResponse>()
            .ToListAsync(cancellationToken);

        public async Task<Result<PollResponse>> GetAsync(int id, CancellationToken cancellationToken = default) {
        
           var poll = await _context.Polls.FindAsync(id, cancellationToken);
            return poll is null
                ? Result.Failure<PollResponse>(PollErrors.PollNotFound)
                : Result.Success(poll.Adapt<PollResponse>()) ;
        }

        public async Task<Result<PollResponse>> AddAsync(PollRequest request, CancellationToken cancellationToken = default)
        {
            var isExistingTitle = await _context.Polls.AnyAsync(p => p.Title == request.Title, cancellationToken);
            if(isExistingTitle) return Result.Failure<PollResponse>(PollErrors.DuplicatedPollTitle);
            var poll = request.Adapt<Poll>();

            await _context.AddAsync(poll, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success(poll.Adapt<PollResponse>());
        }

        public async Task<Result> UpdateAsync(int id, PollRequest request, CancellationToken cancellationToken = default)
        {
            var isExistingTitle = await _context.Polls.AnyAsync(p => p.Title == request.Title && p.Id != id, cancellationToken);

            if(isExistingTitle) return Result.Failure<PollResponse>(PollErrors.DuplicatedPollTitle);

            var currentPoll = await _context.Polls.FindAsync(id, cancellationToken);
            if (currentPoll is null) return Result.Failure(PollErrors.PollNotFound);
            currentPoll.Title = request.Title;
            currentPoll.Summary = request.Summary;
            currentPoll.StartsAt = request.StartsAt;
            currentPoll.EndsAt = request.EndsAt;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var poll = await _context.Polls.FindAsync(id, cancellationToken);
            if (poll is null) return Result.Failure(PollErrors.PollNotFound);
            _context.Remove(poll);
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> TogglePublishStatusAsync(int id, CancellationToken cancellationToken = default)
        {
            var poll = await _context.Polls.FindAsync(id, cancellationToken);
            if (poll is null) return Result.Failure(PollErrors.PollNotFound);
            poll.IsPublished = !poll.IsPublished;
            await _context.SaveChangesAsync(cancellationToken);
            //Send Email
            if (poll.IsPublished && poll.StartsAt == DateOnly.FromDateTime(DateTime.Now))
                BackgroundJob.Enqueue(() => _notificationService1.SendNewPollsNotification(poll.Id));

            return Result.Success();
        }
    }
}
