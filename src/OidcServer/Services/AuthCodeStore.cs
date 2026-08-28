using System.Collections.Concurrent;

namespace OidcServer.Services;

public class AuthCodeStore
{
  private readonly ConcurrentDictionary<string, string> _codes = new();

  public void Save(string code, string value)
  {
    _codes[code] = value;
  }

  public bool TryGet(string code, out string value)
  {
    return _codes.TryGetValue(code, out value);
  }
}
