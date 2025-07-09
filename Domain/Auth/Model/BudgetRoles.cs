namespace BudgetBuilder.Domain.Auth.Model
{
    public static class BudgetRoles
    {
        public const string Admin = nameof(Admin);
        public const string CompanyManager = nameof(CompanyManager);
        public const string BudgetUser = nameof(BudgetUser);

        public static readonly IReadOnlyCollection<string> All = new[] { Admin, CompanyManager, BudgetUser };
    }
}
