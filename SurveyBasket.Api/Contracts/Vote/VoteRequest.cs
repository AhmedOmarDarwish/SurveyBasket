namespace SurveyBasket.Contracts.Vote
{
    public record VoteRequest
    (
        IEnumerable<VoteAnswerRequest> Answers      
    );
}
