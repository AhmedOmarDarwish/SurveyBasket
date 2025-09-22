
namespace SurveyBasket.Services
{
    public class QuestionService(ApplicationDbContext context) : IQuestionService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<Result<IEnumerable<QuestionResponse>>> GetAllAsync(int pollId, CancellationToken cancellationToken = default)
        {
            var pollIsExists = await _context.Polls.AnyAsync(p => p.Id == pollId, cancellationToken);
            if (!pollIsExists) return Result.Failure<IEnumerable<QuestionResponse>>(PollErrors.PollNotFound);

            var questions = await _context.Questions
                .Where(q => q.PollId == pollId)
                .Include(a => a.Answers)
                //Way 1
                //.Select(q => new QuestionResponse(
                //    q.Id,
                //    q.Content,
                //    q.Answers.Select(a => new AnswerResponse(a.Id, a.Content))
                //))
                //Way 2
                .ProjectToType<QuestionResponse>()

                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<QuestionResponse>>(questions);
        }

        public async Task<Result<IEnumerable<QuestionResponse>>> GetAvailableAsync(int pollId, string userId, CancellationToken cancellationToken = default)
        {
            var hasVote = await _context.Votes.AnyAsync(v => v.PollId == pollId && v.UserId == userId, cancellationToken);
            if(hasVote) return Result.Failure<IEnumerable<QuestionResponse>>(VoteErrors.DuplicatedVote);
           
            var pollIsExists = await _context.Polls.AnyAsync(
                p => p.Id == pollId 
                && p.IsPublished 
                && p.StartsAt <= DateOnly.FromDateTime(DateTime.Now) 
                && p.EndsAt >= DateOnly.FromDateTime(DateTime.Now)
                , cancellationToken);
            if (!pollIsExists) return Result.Failure<IEnumerable<QuestionResponse>>(PollErrors.PollNotFound);

            var questions = await _context.Questions
                .Where(q => q.PollId == pollId && q.IsActive)
                .Include(q => q.Answers)
                .Select(q => new QuestionResponse
                    (
                        q.Id,
                        q.Content,
                        q.Answers.Where(a=> a.IsActive).Select(a=> new AnswerResponse(a.Id, a.Content))
                    ))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<QuestionResponse>>(questions);
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

            request.Answers.ForEach(answer => question.Answers.Add(new Answer { Content = answer }));
            await _context.AddAsync(question, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

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
            if(questionIsExists) return Result.Failure(QuestionErrors.DuplicatedQuestionContent);

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
                answer.IsActive =  request.Answers.Contains(answer.Content);
            });

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> TogglePublishStatusAsync(int pollId, int id, CancellationToken cancellationToken = default)
        {
            var question = await _context.Questions.SingleOrDefaultAsync(q => q.PollId == pollId && q.Id == id, cancellationToken);

            if (question is null)
                return Result.Failure(QuestionErrors.QuestionNotFound);
            question.IsActive = !question.IsActive;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
