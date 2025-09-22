namespace SurveyBasket.Mapping
{
    public class MappingConfigurations : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<QuestionRequest, Question>()
                .Ignore(nameof(Question.Answers));
        }
    }
}
