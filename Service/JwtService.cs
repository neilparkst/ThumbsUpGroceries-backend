using System.IdentityModel.Tokens.Jwt;

namespace ThumbsUpGroceries_backend.Service
{
    public class JwtService
    {
        public static string GetClaimFromToken(string token, string claimType)
        {
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring(7);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Return the claim value or null if not found
            return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        public static Dictionary<string, string> GetAllClaimsFromToken(string token)
        {
            if (token.StartsWith("Bearer "))
            {
                token = token.Substring(7);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            return jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
        }
    }
}
