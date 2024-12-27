using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;

namespace FooBooRealTime_back_dotnet.Utils.Generator
{
    public static class UserIdGenerator
    {
        public static Guid ToGuidId(this string stringId)
        {
            using (var sha1 = SHA1.Create())
            {
                // Compute hash of the input string
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(stringId));

                // Take the first 16 bytes of the hash to create a Guid
                return new Guid(hash.Take(16).ToArray());
            }
        }

        public static Guid ToGuidId(this HubCallerContext context)
        {
            var auth0UserIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            var auth0UserId = auth0UserIdClaim?.Value;
            // map the user id into a guid.
            if (auth0UserId == null)
            {
                // Handle the case where the claim is not found
                throw new Exception("User identifier claim not found");
            }
            // map the Auth0 specific (Auth0|12321...) into a Guid
            // (**) note: this occur because i start to use Auth0 which dont use a Guid Id system. as oppose to my existed system.
            Guid targetId = auth0UserId.ToGuidId();
            return targetId;
        }
    }
}
