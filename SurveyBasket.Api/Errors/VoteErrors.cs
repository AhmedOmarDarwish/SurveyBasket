namespace SurveyBasket.Errors
{
    public static class VoteErrors
    {
        public static readonly Error InvalidQuestions =
         new("Vote.InvalidQuestions", "Invalid Questions", StatusCodes.Status400BadRequest);

        public static readonly Error DuplicatedVote =
        new("Vote.DuplicatedVote", "This user already voted before for this poll", StatusCodes.Status409Conflict);


    }
}
