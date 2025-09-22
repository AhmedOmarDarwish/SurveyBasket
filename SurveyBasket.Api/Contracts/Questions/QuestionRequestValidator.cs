namespace SurveyBasket.Contracts.Questions
{
    public class QuestionRequestValidator : AbstractValidator<QuestionRequest>
    {
        public QuestionRequestValidator()
        {
            RuleFor(q => q.Content)
                .NotEmpty()
                .Length(3, 1000);

            RuleFor(q => q.Answers)
                .NotNull();

            RuleFor(q => q.Answers)
                .Must(q => q.Count > 1)
                .WithMessage("Question Should has at least 2 answers")
                .When(q => q.Answers != null);

            RuleFor(q => q.Answers)
                .Must(q => q.Distinct().Count() == q.Count)
                .WithMessage("You cannot add duplicated answers for the same question")
                .When(q => q.Answers != null);

        }
    }
}
