# Microsoft.Health.Client

The purpose of this library is to serve as the base for creating web clients that target the various Microsoft Azure Health Data Services.

## Example Authenticated Usage

Configuration
```json
{
    "Dicom": {
        "Endpoint": "https://localhost:63838",
        "Authentication": {
            "Enabled": true,
            "AuthenticationType": "OAuth2ClientCredential",
            "OAuth2ClientCredential": {
                "TokenUri": "https://localhost:63838/connect/token",
                "Resource": "health-api",
                "Scope": "health-api",
                "ClientId": "globalAdminServicePrincipal",
                "ClientSecret": "globalAdminServicePrincipal"
            }
        }
    }
}
```

Registration Code
```csharp
IConfigurationSection dicomWebConfigurationSection = _configuration.GetSection("Dicom");
services.AddOptions<DicomWebConfiguration>().Bind(dicomWebConfigurationSection);

 services.AddHttpClient<IDicomWebClient, DicomWebClient>((sp, client) =>
    {
        DicomWebConfiguration config = sp.GetRequiredService<IOptions<DicomWebConfiguration>>().Value;
        client.BaseAddress = config.Endpoint;
    })
    .AddPolicyHandler(retryPolicy)
    .AddAuthenticationHandler(dicomWebConfigurationSection.GetSection(AuthenticationConfiguration.SectionName), "Dicom");
```
