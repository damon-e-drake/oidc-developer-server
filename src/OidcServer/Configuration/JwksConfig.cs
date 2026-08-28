using System.Collections.Generic;
using Newtonsoft.Json;

namespace OidcServer.Configuration;

public class JwksConfig
{
  [JsonProperty("keys")]
  public List<JwksKey> Keys { get; set; } = [];
}

public class JwksKey
{
  [JsonProperty("kty")]
  public string KeyType { get; set; }

  [JsonProperty("kid")]
  public string KeyId { get; set; }

  [JsonProperty("use")]
  public string Use { get; set; }

  [JsonProperty("alg")]
  public string Algorithm { get; set; }

  [JsonProperty("n")]
  public string Modulus { get; set; }

  [JsonProperty("e")]
  public string Exponent { get; set; }
}
