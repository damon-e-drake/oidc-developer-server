# Mock OIDC Server

This project is a small ASP.NET Core OIDC mock server that exposes the standard discovery, authorization, token, userinfo, and JWKS endpoints used in OpenID Connect testing.

## Dependencies

- **.NET 10**
- **System.IdentityModel.Tokens.Jwt** (8.3.0) - JWT token generation and validation
- **Newtonsoft.Json** (13.0.3) - JSON serialization and deserialization

## Features

- OpenID Connect discovery document
- Authorization endpoint
- Token endpoint
- Userinfo endpoint
- JWKS endpoint
- Multiple users loaded from JSON
- Configuration via Options pattern
- Strongly-typed configuration models

## Run locally

From the project folder:

```powershell
dotnet run --launch-profile OidcServer
```

The app listens on:

```text
http://localhost:4000
```

## Testing with test.http

The project includes a `test.http` file for easy API testing directly in Visual Studio.

### How to use:

1. **Start the server**: Run `dotnet run --launch-profile OidcServer`

2. **Open test.http** in Visual Studio

3. **Run any authorization request** (e.g., Request #3 for Bugs Bunny):
   - Click "Send Request" above the request
   - The response automatically captures the authorization code

4. **Run the corresponding token request** (e.g., Request #4 for Bugs Bunny):
   - The code is automatically extracted from the previous response
   - Returns `access_token` and `id_token`
   - Decode the JWT at https://jwt.io to inspect claims

5. **Test different users** by running their respective authorization + token request pairs:
   - Bugs Bunny: Requests #3 & #4
   - Daffy Duck: Requests #5 & #6
   - Tweety Bird: Requests #7 & #8
   - And more...

## Endpoints

### Discovery

```text
GET /.well-known/openid-configuration
```

### Authorization

```text
GET /authorize?response_type=code&client_id=mock-client&redirect_uri=https://localhost:9443/oauth/callback&state=test&scope=openid
```

### Token

```text
POST /token
Content-Type: application/x-www-form-urlencoded
```

Form data:

```text
grant_type=authorization_code
code=<code from authorize response>
redirect_uri=https://localhost:9443/oauth/callback
client_id=mock-client
client_secret=mock-secret
```

### Userinfo

```text
GET /userinfo
```

### JWKS

```text
GET /jwks
```

## Configuration

Configuration is loaded from:

- `appsettings.json` - main settings
- `data/jwks.json` - JWKS keys
- `data/oidc-config.json` - discovery metadata
- `data/users.json` - user accounts

### appsettings.json

```json
{
  "OidcSettings": {
    "Port": "4000",
    "Issuer": "http://localhost:4000",
    "ClientId": "mock-client",
    "ClientSecret": "mock-secret",
    "RedirectUri": "https://localhost:9443/oauth/callback",
    "SecretPhrase": "bmoJ5rxzOZWwMY8KXt4SP0EsNDfkvaplj2Li7U6Vyuc9TIQ3gABhRdCGnqFeH1"
  }
}
```

**SecretPhrase**: A 64-character random alphanumeric string used for HMAC-SHA256 JWT signing.

**Dynamic User Lookup**: The server uses the `client_id` from the authorization request to determine which user to authenticate as. The `client_id` is matched against the `username` field in the user data. For example:
- `client_id=bugs` → matches `username: "bugs"` → generates token for Bugs Bunny
- `client_id=daffy` → matches `username: "daffy"` → generates token for Daffy Duck
- `client_id=unknown` → no match found → falls back to the first available user

### data/users.json

User accounts with claims and credentials:

```json
[
  {
    "subject": "bugs",
    "username": "bugs",
    "email": "bugs.bunny@example.com",
    "name": "Bugs Bunny",
    "givenName": "Bugs",
    "familyName": "Bunny",
    "password": "Password123!",
    "additionalClaims": {
      "nickname": "Bugs",
      "preferred_username": "bugs",
      "email_verified": true
    }
  }
]
```

### data/jwks.json

JWKS key configuration:

```json
{
  "keys": [
    {
      "kty": "RSA",
      "kid": "mock-key",
      "use": "sig",
      "alg": "RS256",
      "n": "mockn",
      "e": "AQAB"
    }
  ]
}
```

### data/oidc-config.json

Discovery endpoint metadata:

```json
{
  "supportedScopes": ["openid", "profile", "email"],
  "supportedClaims": ["iss", "sub", "aud", "exp", "iat", ...],
  "responseTypesSupported": ["code", "token", "id_token"],
  "grantTypesSupported": ["authorization_code", "implicit", "refresh_token"]
}
```

## Built-in users

Default users included (10 classic cartoon characters):

**Looney Tunes:**
- `bugs` (Bugs Bunny)
- `daffy` (Daffy Duck)
- `tweety` (Tweety Bird)
- `sylvester` (Sylvester Cat)
- `porky` (Porky Pig)

**The Flintstones:**
- `fred` (Fred Flintstone)
- `barney` (Barney Rubble)

**The Jetsons:**
- `george` (George Jetson)
- `elroy` (Elroy Jetson)

**Captain Caveman:**
- `caveman` (Captain Caveman)

Add more users by editing `data/users.json`.

## Project Structure

```
OidcServer/
├── Configuration/          # Configuration models
│   ├── JwksConfig.cs
│   ├── OidcDiscoveryConfig.cs
│   └── OidcSettings.cs
├── Controllers/           # API endpoints
│   └── OidcController.cs
├── Models/               # Domain models
│   └── OidcUser.cs
├── Services/             # Business logic
│   ├── AuthCodeStore.cs
│   └── OidcUserStore.cs
├── data/                 # JSON configuration files
│   ├── jwks.json
│   ├── oidc-config.json
│   └── users.json
├── Program.cs            # Application entry point
└── appsettings.json      # App configuration
```

## Development notes

- Nullable is disabled
- Implicit usings are disabled
- Uses ASP.NET Core Options pattern
- Configuration loaded via IOptions<T>
- Uses Newtonsoft.Json for serialization

