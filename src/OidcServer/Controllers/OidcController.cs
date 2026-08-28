using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OidcServer.Configuration;
using OidcServer.Services;

namespace OidcServer.Controllers;

[ApiController]
public class OidcController(IOptions<OidcSettings> settings, OidcUserStore userStore, AuthCodeStore authCodeStore, ILogger<OidcController> logger) : ControllerBase
{
  private readonly OidcSettings _settings = settings.Value;
  private readonly OidcUserStore _userStore = userStore;
  private readonly AuthCodeStore _authCodeStore = authCodeStore;
  private readonly ILogger<OidcController> _logger = logger;

  [HttpGet("/.well-known/openid-configuration")]
  public IActionResult Discovery()
  {
    return Ok(new
    {
      issuer = _settings.Issuer,
      authorization_endpoint = $"{_settings.Issuer}/authorize",
      token_endpoint = $"{_settings.Issuer}/token",
      userinfo_endpoint = $"{_settings.Issuer}/userinfo",
      jwks_uri = $"{_settings.Issuer}/jwks",
      registration_endpoint = $"{_settings.Issuer}/register",
      scopes_supported = _settings.Discovery.SupportedScopes,
      response_types_supported = _settings.Discovery.ResponseTypesSupported,
      grant_types_supported = _settings.Discovery.GrantTypesSupported,
      subject_types_supported = _settings.Discovery.SubjectTypesSupported,
      id_token_signing_alg_values_supported = _settings.Discovery.IdTokenSigningAlgValuesSupported,
      token_endpoint_auth_methods_supported = _settings.Discovery.TokenEndpointAuthMethodsSupported,
      claims_supported = _settings.Discovery.SupportedClaims
    });
  }

  [HttpGet("/authorize")]
  public IActionResult Authorize()
  {
    var responseType = Request.Query["response_type"].ToString();
    var clientId = Request.Query["client_id"].ToString();
    var redirectUri = Request.Query["redirect_uri"].ToString();
    var state = Request.Query["state"].ToString();
    var scope = Request.Query["scope"].ToString();

    if (redirectUri != _settings.RedirectUri)
      return BadRequest("Invalid redirect_uri");

    if (string.IsNullOrWhiteSpace(responseType) || string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(clientId))
      return BadRequest();

    var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    _authCodeStore.Save(code, clientId);

    return Redirect($"{_settings.RedirectUri}?code={code}&state={state}");
  }

  [HttpGet("/test/authorize")]
  public IActionResult TestAuthorize()
  {
    var responseType = Request.Query["response_type"].ToString();
    var clientId = Request.Query["client_id"].ToString();
    var state = Request.Query["state"].ToString();
    var scope = Request.Query["scope"].ToString();

    if (string.IsNullOrWhiteSpace(responseType) || string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(clientId))
      return BadRequest("Missing required parameters");

    var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
    _authCodeStore.Save(code, clientId);

    return Ok(new
    {
      code,
      state,
      client_id = clientId
    });
  }

  [HttpPost("/token")]
  public IActionResult Token()
  {
    var form = Request.Form;
    var grantType = form["grant_type"].ToString();
    var code = form["code"].ToString();
    var redirectUri = form["redirect_uri"].ToString();
    var clientId = form["client_id"].ToString();
    var clientSecret = form["client_secret"].ToString();

    if (grantType != "authorization_code" || clientSecret != _settings.ClientSecret || redirectUri != _settings.RedirectUri)
      return BadRequest(new { error = "invalid_grant" });

    if (!_authCodeStore.TryGet(code, out var storedClientId))
      return BadRequest(new { error = "invalid_grant" });

    var accessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    var idToken = BuildIdToken(storedClientId);

    if (_logger.IsEnabled(LogLevel.Information))
      _logger.LogInformation("Issued access token: {AccessToken}, id token: {IdToken} for client: {ClientId}", accessToken, idToken, storedClientId);

    return Ok(new
    {
      access_token = accessToken,
      id_token = idToken,
      token_type = "Bearer",
      expires_in = 3600
    });
  }

  [HttpGet("/userinfo")]
  public IActionResult UserInfo()
  {
    var username = User?.FindFirstValue("preferred_username");
    var user = !string.IsNullOrWhiteSpace(username)
      ? _userStore.GetByUsername(username) ?? _userStore.GetBySubject(username)
      : null;

    user ??= _userStore.All().FirstOrDefault();

    if (user == null)
      return NotFound();

    return Ok(new
    {
      sub = user.Subject,
      email = user.Email,
      name = user.Name
    });
  }

  [HttpGet("/jwks")]
  public IActionResult Jwks()
  {
    return Ok(_settings.Jwks);
  }

  private string BuildIdToken(string clientId)
  {
    var user = _userStore.GetByUsername(clientId)
      ?? _userStore.All().FirstOrDefault()
      ?? throw new InvalidOperationException($"No users found in user store");
    var now = DateTime.UtcNow;
    var claims = new List<Claim>
    {
      new("sub", user.Subject),
      new("exp", ((DateTimeOffset)now.AddHours(1)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
      new("iat", ((DateTimeOffset)now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
      new("email", user.Email),
      new("name", user.Name),
      new("given_name", user.GivenName),
      new("family_name", user.FamilyName),
      new("auth_time", ((DateTimeOffset)now.AddMinutes(-100)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
      new("nonce", "mock-nonce"),
      new("jti", Convert.ToHexString(RandomNumberGenerator.GetBytes(8))),
      new("ver", "1")
    };

    if (user.AdditionalClaims != null)
    {
      foreach (var additionalClaim in user.AdditionalClaims)
      {
        if (additionalClaim.Value is JArray array)
        {
          var arrayValue = JsonConvert.SerializeObject(array, Formatting.None);
          claims.Add(new Claim(additionalClaim.Key, arrayValue, JsonClaimValueTypes.JsonArray));
        }
        else
        {
          var value = additionalClaim.Value?.ToString() ?? string.Empty;
          claims.Add(new Claim(additionalClaim.Key, value));
        }
      }
    }

    var token = new JwtSecurityToken(
        issuer: "oidc-developer-auth",
        audience: "oidc-developer-auth",
        claims: claims,
        notBefore: now.AddMinutes(-1),
        expires: now.AddHours(1),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretPhrase)), SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
