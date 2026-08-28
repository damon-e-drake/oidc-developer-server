using System.Collections.Generic;
using Newtonsoft.Json;

namespace OidcServer.Configuration;

public class OidcDiscoveryConfig
{
  [JsonProperty("supportedScopes")]
  public List<string> SupportedScopes { get; set; } = [];

  [JsonProperty("supportedClaims")]
  public List<string> SupportedClaims { get; set; } = [];

  [JsonProperty("responseTypesSupported")]
  public List<string> ResponseTypesSupported { get; set; } = [];

  [JsonProperty("grantTypesSupported")]
  public List<string> GrantTypesSupported { get; set; } = [];

  [JsonProperty("subjectTypesSupported")]
  public List<string> SubjectTypesSupported { get; set; } = [];

  [JsonProperty("idTokenSigningAlgValuesSupported")]
  public List<string> IdTokenSigningAlgValuesSupported { get; set; } = [];

  [JsonProperty("tokenEndpointAuthMethodsSupported")]
  public List<string> TokenEndpointAuthMethodsSupported { get; set; } = [];
}
