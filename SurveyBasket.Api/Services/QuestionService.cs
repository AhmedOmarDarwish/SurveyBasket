using Microsoft.Extensions.Caching.Hybrid;
using System.Linq.Dynamic.Core;
using SurveyBasket.Contracts.Common;

namespace SurveyBasket.Services
{
    public class QuestionService(ApplicationDbContext context, ICacheService cacheService, HybridCache hybridCache, ILogger<QuestionService> logger) : IQuestionService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ICacheService _cacheService = cacheService;
        private readonly HybridCache _hybridCache = hybridCache;
        private readonly ILogger<QuestionService> _logger = logger;
        private const string _cachePrefix = "availableQuestions";

        public async Task<Result<PaginatedList<QuestionResponse>>> GetAllAsync(int pollId,RequestFilters filters, CancellationToken cancellationToken = default)
        {
            var pollIsExists = await _context.Polls.AnyAsync(p => p.Id == pollId, cancellationToken);
            if (!pollIsExists) return Result.Failure<PaginatedList<QuestionResponse>>(PollErrors.PollNotFound);

            var query = _context.Questions
                .Where(q => q.PollId == pollId);

            if (!string.IsNullOrEmpty(filters.SearchValue))
                query = query.Where((q => q.Content.Contains(filters.SearchValue)));

            if (!string.IsNullOrEmpty(filters.SortColumn))
                query = query.OrderBy($"{filters.SortColumn} {filters.SortDirection}");

            var source = query.Include(a => a.Answers)
                .ProjectToType<QuestionResponse>()
                .AsNoTracking();

            var questions = await PaginatedList<QuestionResponse>.CreateAsync(source, filters.PageNumber, filters.PageSize, cancellationToken);

            return Result.Success(questions);
        }

        public async Task<Result<IEnumerable<QuestionResponse>>> GetAvailableAsync(int pollId, string userId, CancellationToken cancellationToken = default)
        {
            var hasVote = await _context.Votes.AnyAsync(v => v.PollId == pollId && v.UserId == userId, cancellationToken);
            if (hasVote) return Result.Failure<IEnumerable<QuestionResponse>>(VoteErrors.DuplicatedVote);

            var pollIsExists = await _context.Polls.AnyAsync(
                p => p.Id == pollId
                && p.IsPublished
                && p.StartsAt <= DateOnly.FromDateTime(DateTime.Now)
                && p.EndsAt >= DateOnly.FromDateTime(DateTime.Now)
                , cancellationToken
            );

            if (!pollIsExists) return Result.Failure<IEnumerable<QuestionResponse>>(PollErrors.PollNotFound);

            //Use DistributedMemoryCache
            //var cacheKey = $"{_cachePrefix}-{pollId}";
            //var cachedQuestion = await _cacheService.GetAsync<IEnumerable<QuestionResponse>>(cacheKey, cancellationToken);
            //IEnumerable<QuestionResponse> questions = [];
            //if (cachedQuestion is null)
            //{
            //    _logger.LogInformation(message: "Select Questions From Database");
            //    questions = await _context.Questions
            //    .Where(q => q.PollId == pollId && q.IsActive)
            //    .Include(q => q.Answers)
            //    .Select(q => new QuestionResponse
            //        (
            //            q.Id,
            //            q.Content,
            //            q.Answers.Where(a => a.IsActive).Select(a => new AnswerResponse(a.Id, a.Content))
            //        ))
            //    .AsNoTracking()
            //    .ToListAsync(cancellationToken);

            //    await _cacheService.SetAsync(cacheKey, questions, cancellationToken);
            //}
            //else
            //{
            //    _logger.LogInformation(message: "Get Questions From Cache");
            //    questions = cachedQuestion;
            //}

            //Use Hybrid Cache
            var cacheKey = $"{_cachePrefix}-{pollId}";
            var questions = await _hybridCache.GetOrCreateAsync<IEnumerable<QuestionResponse>>(
                cacheKey,
                async cacheEntry => await _context.Questions
                    .Where(x => x.PollId == pollId && x.IsActive)
                    .Include(x => x.Answers)
                    .Select(q => new QuestionResponse(
                        q.Id,
                        q.Content,
                        q.Answers.Where(a => a.IsActive).Select(a => new AnswerResponse(a.Id, a.Content))
                    ))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken)//,
                        // new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(30) }
            );


            //var questions = await _context.Questions
            //    .Where(q => q.PollId == pollId && q.IsActive)
            //    .Include(q => q.Answers)
            //    .Select(q => new QuestionResponse
            //        (
            //            q.Id,
            //            q.Content,
            //            q.Answers.Where(a => a.IsActive).Select(a => new AnswerResponse(a.Id, a.Content))
            //        ))
            //    .AsNoTracking()
            //    .ToListAsync(cancellationToken);

            return Result.Success(questions!);
        }

        public async Task<Result<QuestionResponse>> GetAsync(int pollId, int id, CancellationToken cancellationToken = default)
        {
            var question = await _context.Questions
                .Where(q => q.PollId == pollId && q.Id == id)
                .Include(a => a.Answers)
                .ProjectToType<QuestionResponse>()
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);

            return question is null ? Result.Failure<QuestionResponse>(QuestionErrors.QuestionNotFound) : Result.Success(question);
        }

        public async Task<Result<QuestionResponse>> AddAsync(int pollId, QuestionRequest request, CancellationToken cancellationToken = default)
        {
            var pollIsExists = await _context.Polls.AnyAsync(p => p.Id == pollId, cancellationToken);

            if (!pollIsExists) return Result.Failure<QuestionResponse>(PollErrors.PollNotFound);

            var questionIsExists = await _context.Questions.AnyAsync(p => p.Content == request.Content && p.PollId == pollId, cancellationToken);
            if (questionIsExists)
                return Result.Failure<QuestionResponse>(QuestionErrors.DuplicatedQuestionContent);

            var question = request.Adapt<Question>();
            question.PollId = pollId;

            //request.Answers.ForEach(answer => question.Answers.Add(new Answer { Content = answer }));
            await _context.AddAsync(question, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            //DistributedMemoryCache
            // await _cacheService.RemoveAsync($"{_cachePrefix}-{pollId}", cancellationToken);

            //Hybrid Cache
            await _hybridCache.RemoveAsync($"{_cachePrefix}-{pollId}", cancellationToken);

            return Result.Success(question.Adapt<QuestionResponse>());
        }

        public async Task<Result> UpdateAsync(int pollId, int id, QuestionRequest request, CancellationToken cancellationToken = default)
        {
            //Check if Content is already in poll Questions
            var questionIsExists = await _context.Questions
                .AnyAsync(q => q.PollId == pollId
                    && q.Id != id
                    && q.Content == request.Content
                    , cancellationToken
                );
            if (questionIsExists) return Result.Failure(QuestionErrors.DuplicatedQuestionContent);

            var question = await _context.Questions
                .Include(q => q.Answers)
                .SingleOrDefaultAsync(q => q.PollId == pollId && q.Id == id, cancellationToken: cancellationToken);
            if (question is null) return Result.Failure(QuestionErrors.QuestionNotFound);

            question.Content = request.Content;
            //Current Answers
            var currentAnswer = question.Answers.Select(answer => answer.Content).ToList();

            //add new Answers
            var newAnswers = request.Answers.Except(currentAnswer).ToList();
            newAnswers.ForEach(answer =>
            {
                question.Answers.Add(new Answer { Content = answer });
            });
            //Change Active or Not Active if Send make it active if not send make it not active
            //All Question old and new
            question.Answers.ToList().ForEach(answer =>
            {
                answer.IsActive = request.Answers.Contains(answer.Content);
            });

            await _context.SaveChangesAsync(cancellationToken);

            //DistributedMemoryCache
            // await _cacheService.RemoveAsync($"{_cachePrefix}-{pollId}", cancellationToken);

            //Hybrid Cache
            await _hybridCache.RemoveAsync($"{_cachePrefix}-{pollId}", cancellationToken);

            return Result.Success();
        }

        public async Task<Result> TogglePublishStatusAsync(int pollId, int id, CancellationToken cancellationToken = default)
        {
            var question = await _context.Questions.SingleOrDefaultAsync(q => q.PollId == pollId && q.Id == id, cancellationToken);

            if (question is null)
                return Result.Failure(QuestionErrors.QuestionNotFound);
            question.IsActive = !question.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            //DistributedMemoryCache
            //await _cacheService.RemoveAsync($"{_cachePrefix}-{pollId}", cancellationToken);

            //Hybrid Cache
            await _hybridCache.RemoveAsync($"{_cachePrefix}-{pollId}", cancellationToken);

            return Result.Success();
        }
    }
}
