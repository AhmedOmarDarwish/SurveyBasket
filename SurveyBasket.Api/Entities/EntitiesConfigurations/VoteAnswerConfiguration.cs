namespace SurveyBasket.Entities.EntitiesConfigurations
{
    public class VoteAnswerConfiguration : IEntityTypeConfiguration<VoteAnswer>
    {
        public void Configure(EntityTypeBuilder<VoteAnswer> builder)
        {
            builder.HasIndex(v => new { v.VoteId, v.QuestionId }).IsUnique();
        }
    }
}
