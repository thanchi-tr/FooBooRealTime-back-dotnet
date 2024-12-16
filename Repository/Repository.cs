using AutoMapper;
using backend.Model.LogUtil;
using FooBooRealTime_back_dotnet.Data;
using FooBooRealTime_back_dotnet.Interface.Repository;
using FooBooRealTime_back_dotnet.Model.Domain;
using Microsoft.EntityFrameworkCore;

namespace FooBooRealTime_back_dotnet.Repository
{
    public class Repository<TEntity, TDto, TKey> : IRepository<TEntity, TDto, TKey>
        where TEntity : class, IHasStringId
        where TDto : class
    {
        protected readonly ILogger<Repository<TEntity, TDto, TKey>> _logger;
        protected readonly FooBooDbContext _dbContext;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly IMapper _mapper;

        public Repository(ILogger<Repository<TEntity, TDto, TKey>> logger, FooBooDbContext dbContext,  IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _dbSet = dbContext.Set<TEntity>();
            _mapper = mapper;
        }


        /// <summary>
        /// Create the object without any dependancy.
        /// 
        /// log where appropriate
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public virtual async Task<TEntity?> CreateAsync(TDto info)
        {
            TEntity newEntity = _mapper.Map<TEntity>(info);

            try
            {
                await _dbSet.AddAsync(newEntity);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"{this.GetType().Name} Successfully Create an Entity with ID:{newEntity.IdToString}");
                return newEntity;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError($"Database update error: {dbEx.Message}");

            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Try to delete else, log the error
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task DeleteAsync(TKey id)
        {
            TEntity? target = await _dbSet.FindAsync(id);
            if (target != null)
            {
                _dbSet.Remove(target);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"{this.GetType().Name} is Successfully Remove an Entity with ID:{target.IdToString} ");
            }
            else
            {
                _logger.LogInformation($"Attempt to delete a non-existed Item with ID:{id}");
            }
        }

        /// <summary>
        /// Get the Tentity by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual async Task<TEntity?> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }



        /// <summary>
        /// Update item by Id (can update all the index along side)
        /// </summary>
        /// <param name="info"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual async Task<TEntity?> UpdateAsync(TDto info, TKey id)
        {
            TEntity? target = await _dbSet.FindAsync(id);
            if (target == null)
            {
                _logger.LogWarning($"Attempt to update an item with ID {id} failed: Item does not exist.");
                return null;
            }

            // Use reflection to copy properties from TDto to TEntity
            var infoProperties = typeof(TDto).GetProperties();
            var targetProperties = typeof(TEntity).GetProperties().ToDictionary(p => p.Name);

            foreach (var prop in infoProperties)
            {
                if (targetProperties.TryGetValue(prop.Name, out var targetProp) && targetProp.CanWrite)
                {
                    // Only update properties that are not null in the info object
                    var value = prop.GetValue(info);
                    if (value != null && targetProp.PropertyType.IsAssignableFrom(prop.PropertyType))
                    {
                        targetProp.SetValue(target, value);
                    }
                }
            }

            // Save changes
            _dbSet.Update(target);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"{this.GetType().Name} is Successfully Update an Entity with ID:{target.IdToString}");
            return target;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual async Task<TEntity[]> GetAllAsync()
        {
            return await _dbSet.ToArrayAsync();
        }
    }
}
