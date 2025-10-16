namespace SurveyBasket.Abstractions.Consts;

public static class DefaultRoles
{
    public partial class Admin
    {
        public const string Name = nameof(Admin);
        public const string Id = "0199ea7d-3cde-7aaf-8494-9997b72f2dbf";
        public const string ConcurrencyStamp = "0199ea7d-3cde-7050-8002-325426d3111f";
    }

    public partial class Member
    {
        public const string Name = nameof(Member);
        public const string Id = "0199ea7d-3cde-7d12-899f-3c9473118eab";
        public const string ConcurrencyStamp = "0199ea7d-3cde-73f5-9e70-a2aaa2fe2bdf";
    }
}