using AutoMapper;
using FooBooRealTime_back_dotnet.Data;
using FooBooRealTime_back_dotnet.Interface.GameContext;
using FooBooRealTime_back_dotnet.Interface.Repository;
using FooBooRealTime_back_dotnet.Interface.Service;
using FooBooRealTime_back_dotnet.Model.Domain;
using FooBooRealTime_back_dotnet.Model.DTO;
using FooBooRealTime_back_dotnet.Repository;
using FooBooRealTime_back_dotnet.Utils.Validator;
using Microsoft.EntityFrameworkCore;
using Serilog.Debugging;

namespace FooBooRealTime_back_dotnet.Services
{
    public class GameService : Repository<Game, GameDTO, string>, IGameService

    {
        public readonly IGameMaster _gameMaster;

        public GameService(IGameMaster gameMaster,ILogger<Repository<Game, GameDTO, string>> logger, FooBooDbContext dbContext, IMapper mapper) : base(logger, dbContext, mapper)
        {
            _gameMaster = gameMaster;
        }

        public override async Task<Game?> GetByIdAsync(string id)
        {
            var target = await base.GetByIdAsync(id);
            target.AuthorId = null;
            return target;
        }

        /// <summary>
        /// The rule must be validate 
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public override async Task<Game?> CreateAsync(GameDTO info)
        {
            if(!info.Rules.Validate())
                return null;

            return await base.CreateAsync(info);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public override Task<Game?> UpdateAsync(GameDTO info, string id)
        {
            if (!info.Rules.Validate())
                return null;
            return base.UpdateAsync(info, id);
        }

        /// <summary>
        /// Wrapper around the delete request to validate the requestor
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="requestorId"></param>
        /// <returns></returns>
        public async Task<Boolean> DeleteGameRequest(string gameId, Guid requestorId)
        {
            Game? target = await GetByIdAsync(gameId);
            if (target != null &&
                    target.AuthorId != requestorId
                )
            {
                _logger.LogWarning($"{this.GetType().Name} Catch an violation: Attempt to delete un-authorsed resource with ID:{requestorId}");
                return false;
            }

            await DeleteAsync(gameId);
            return true;
        }

        
        /// <summary>
        /// Retrieve all the game of the author
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        public async Task<Game[]> GetAllGameByAuthorId(Guid authorId)
        {
            var games = await _dbSet
                            .Where(g => g.AuthorId == authorId)
                            .ToArrayAsync();
            return games;
        }

        /// <summary>
        /// Delete all the game of author
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task DeleteAllGameByAuthorId(Guid authorId)
        {
            var targets = await GetAllGameByAuthorId(authorId);
            _dbContext.RemoveRange(targets);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"{this.GetType().Name} is Successfullu delte all resources of author with id ID:{authorId}");
        }

        /// <summary>
        /// Make sure the Author Id is not return over API (sensitive data)
        /// </summary>
        /// <returns></returns>
        public async override Task<Game[]> GetAllAsync()
        {
            var targets = await base.GetAllAsync();

            foreach (var g in targets)
            {
                g.AuthorId = null;
            }
            return targets;

        }
    }
}
