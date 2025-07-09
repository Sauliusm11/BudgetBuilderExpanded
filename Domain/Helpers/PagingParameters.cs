namespace BudgetBuilder.Domain.Helpers
{
    public class PagingParameters
    {
        private int _pageSize = 2;
        private int _pageNumber = 1;

        private readonly int _maxPageSize = 50;

        public int? PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value ?? _pageNumber;
        }
        public int? PageSize
        {
            get => _pageSize > _maxPageSize ? _maxPageSize : _pageSize;
            set => _pageSize = value ?? _pageSize;
        }
    }
}
