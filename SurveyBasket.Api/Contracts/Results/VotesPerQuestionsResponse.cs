namespace SurveyBasket.Contracts.Results
{
    public record VotesPerQuestionsResponse
    (
        string Question,
        IEnumerable<VotesPerAnswerResponse> SelectedAnswers
    );
}
