using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace TheGame.Api.Controllers
{
  [ApiController]
  [AllowAnonymous]
  public class AccountController : ControllerBase
  {
    private const string _authTokenName = "id_token";

    public AccountController()
    { }

    [HttpGet]
    [Route("account/google-login")]
    public IActionResult GoogleLogin()
    {
      var properties = new AuthenticationProperties
      {
        RedirectUri = Url.Action("SignInGoogle"),
        AllowRefresh = true,
      };
      return Challenge(properties, "Google");
    }

    [HttpGet]
    [Route("account/signingoogle")]
    public async Task<IActionResult> SignInGoogle()
    {
      var googleAuthResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

      var allTokens = googleAuthResult.Properties.GetTokens();

      var claims = googleAuthResult
        .Principal?
        .Identities?
        .FirstOrDefault()?
        .Claims
        .Select(claim => new
        {
          claim.Issuer,
          claim.OriginalIssuer,
          claim.Type,
          claim.Value
        });

      // TODO create user

      return Ok(new
      {
        claims,
        allTokens
      });
    }
  }
}
