using System.Collections.Generic;

namespace OidcServer.Models;

public class OidcUser
{
  public string Subject { get; set; }
  public string Username { get; set; }
  public string Email { get; set; }
  public string Name { get; set; }
  public string GivenName { get; set; }
  public string FamilyName { get; set; }
  public string Password { get; set; }
  public Dictionary<string, object> AdditionalClaims { get; set; } = [];
}
