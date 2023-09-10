using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TheGame.Api.Auth;

namespace TheGame.Api.Controllers
{
  [ApiController]
  public class AccountController : ControllerBase
  {
    private readonly GameAuthService _gameAuthService;

    public AccountController(GameAuthService gameAuthService)
    {
      _gameAuthService = gameAuthService;
    }

    [HttpGet]
    [Route("account/google-login")]
    [AllowAnonymous]
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
    [AllowAnonymous]
    public async Task<IActionResult> SignInGoogle()
    {
      var googleAuthResult = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);

      // Parse auth results
      var claimsPrincipalResult = await _gameAuthService.CreateClaimsIdentity(googleAuthResult);

      if (!claimsPrincipalResult.TryPickT0(out var claimsPrincipal, out var principalError))
      {
        return BadRequest(principalError);
      }

      // create auth cookie
      var authProperties = new AuthenticationProperties
      {
        AllowRefresh = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
        IsPersistent = true,
        IssuedUtc = DateTimeOffset.UtcNow,
      };

      // Remove OpenId challenge cookie
      await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

      // Sign-in
      await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        claimsPrincipal,
        authProperties);

      return Redirect("info");
    }

    [HttpGet]
    [Route("account/info")]
    public IActionResult Info()
    {
      var claimsInfo = HttpContext.User.Claims
        .Select(claim => new
        {
          claim.Type,
          claim.Value,
          claim.Issuer
        })
        .ToList();

      return Ok(claimsInfo);
    }
  }
}
