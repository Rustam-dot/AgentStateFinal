using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using AgencyRealEstate.WebUI.Services;

namespace AgencyRealEstate.WebUI.Services;

public class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly TokenStorage _tokenStorage;

    public TokenAuthenticationStateProvider(TokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = _tokenStorage.Token; 
            if (string.IsNullOrEmpty(token))
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
            }
        }
        catch
        {
            
        }
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}