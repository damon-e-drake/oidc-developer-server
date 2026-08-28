namespace OidcServer.Configuration;

public class OidcSettings
{
  public string Port { get; set; }
  public string Issuer { get; set; }
  public string ClientSecret { get; set; }
  public string RedirectUri { get; set; }
  public string SecretPhrase { get; set; }
  public JwksConfig Jwks { get; set; }
  public OidcDiscoveryConfig Discovery { get; set; }
}
