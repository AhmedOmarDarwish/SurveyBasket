namespace SurveyBasket.Entities

{
    public sealed class Poll
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summery { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateOnly StartsAt { get; set; }
        public DateOnly EndsAt { get; set; }


    }
}
