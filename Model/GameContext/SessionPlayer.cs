using FooBooRealTime_back_dotnet.Interface.Service;

namespace FooBooRealTime_back_dotnet.Model.GameContext
{
    public class SessionPlayer
    {
        protected SessionPlayer(Guid internalId, string? connectionId, string name)
        {
            InternalId = internalId;
            ConnectionId = connectionId;
            IsConnected = true;
            NickName = Name = name;
        }

        public Guid InternalId {  get; set; }
        public string? ConnectionId { get; set; }
        public bool IsConnected { get; set; }
        public string Name { get; set; }
        public string NickName { get; set; }

        /// <summary>
        /// Factory Method 
        /// </summary>
        /// <param name="internalId"></param>
        /// <param name="connectionId"></param>
        /// <param name="playerService"></param>
        /// <returns></returns>
        public static async Task<SessionPlayer?> CreateAsync(Guid internalId,string? connectionId, IPlayerService playerService)
        {
            var targetDomainPlayer = await playerService.GetByIdAsync(internalId);
            if (targetDomainPlayer == null) 
                return null;
            
            return new SessionPlayer(internalId, connectionId, targetDomainPlayer.Name);
        }

        

    }
    
}
