using FooBooRealTime_back_dotnet.Model.Domain;

namespace FooBooRealTime_back_dotnet.Interface.Repository
{
    public interface IRepository<TEntity, TDto, TKey>
        where TEntity : class
        where TDto : class
    {
        public Task<TEntity?> CreateAsync(TDto info);

        public Task<TEntity?> UpdateAsync(TDto info, TKey id);

        public Task DeleteAsync(TKey id);

        public Task<TEntity?> GetByIdAsync(TKey id);

        public Task<TEntity[]> GetAllAsync();
    }
}
