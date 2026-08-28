using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OidcServer.Models;

namespace OidcServer.Services;

public class OidcUserStore
{
  private readonly Dictionary<string, OidcUser> _users;

  public OidcUserStore()
  {
    var jsonPath = Path.Combine(AppContext.BaseDirectory, "data", "users.json");
    var fullPath = Path.GetFullPath(jsonPath);

    if (!File.Exists(fullPath))
      throw new FileNotFoundException("User data file not found at " + fullPath, fullPath);

    var json = File.ReadAllText(fullPath);
    var users = JsonConvert.DeserializeObject<List<OidcUser>>(json);

    _users = new Dictionary<string, OidcUser>(StringComparer.OrdinalIgnoreCase);

    if (users != null)
    {
      foreach (var user in users)
      {
        _users[user.Username] = user;
      }
    }
  }

  public OidcUser GetByUsername(string username)
  {
    if (_users.TryGetValue(username, out var user))
      return user;

    return null;
  }

  public OidcUser GetBySubject(string subject)
  {
    foreach (var user in _users.Values)
    {
      if (string.Equals(user.Subject, subject, StringComparison.OrdinalIgnoreCase))
        return user;
    }

    return null;
  }

  public IReadOnlyCollection<OidcUser> All()
  {
    return _users.Values;
  }
}
