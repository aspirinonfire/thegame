using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TheGame.Api.Security.Models;

namespace TheGame.Api.Controllers
{
  [ApiController]
  [AllowAnonymous]
  public class AccountController : ControllerBase
  {
    private readonly SignInManager<AppUser> _signinManager;

    public AccountController(SignInManager<AppUser> signinManager)
    {
      _signinManager = signinManager;
    }

    [HttpGet]
    [Route("account/google-login")]
    public IActionResult GoogleLogin()
    {
      var properties = new AuthenticationProperties();
      return new ChallengeResult(GoogleDefaults.AuthenticationScheme, properties);
    }

    [HttpGet]
    [Route("account/signingoogle")]
    public async Task<IActionResult> SignInGoogle()
    {
      var info = await _signinManager.GetExternalLoginInfoAsync();

      if (info == null)
      {
        return Ok(null);
      }

      var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

      var claims = result
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

      return Ok(claims);
    }
  }
}
