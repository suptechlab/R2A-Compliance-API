using System;

namespace R2A.ReportApi.Client.Common
{
    public abstract class PagedQueryDto<T> 
    {
        public T Query { get; set; }
        public int Offset { get; set; }
        public int Fetch { get; set; }

        public bool ShouldOffset => Offset > 0 || Fetch > 0;
        public bool ShouldFetch => Fetch > 0;

        public bool ShouldQuery => IsQueryValid(Query);

        protected abstract bool IsQueryValid(T query);

        public void Paged(int pageNumber, int pageSize, T query = default(T))
        {
            if (pageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber,
                    "Page number must be greater than or equal to 1.");
            if (pageSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize,
                    "Page size must be greater than or equal to 1.");

            Query = query;
            Offset = pageSize * (pageNumber - 1);
            Fetch = pageSize;
        }



        public void All(T query = default(T))
        {
            Offset = 0;
            Fetch = 0;
            Query = query;
        }
        
    }

}
