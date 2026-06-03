namespace Sanalink.API.Infrastructure;

public static class FeatureFlags
{
    // ISO/IEC 27001 controls
    public const string ISO27001_Https           = "ISO27001_Https";
    public const string ISO27001_SwaggerGate     = "ISO27001_SwaggerGate";
    public const string ISO27001_RateLimiting    = "ISO27001_RateLimiting";
    public const string ISO27001_AccountLockout  = "ISO27001_AccountLockout";
    public const string ISO27001_TokenRevocation = "ISO27001_TokenRevocation";
    public const string ISO27001_DataRetention   = "ISO27001_DataRetention";

    // Standards
    public const string ICD10 = "ICD10";
    public const string FHIR  = "FHIR";
    public const string DHIS2 = "DHIS2";

    // Rate limiter policy name (used by both Program.cs and AuthController attribute)
    public const string RatePolicy_AuthLogin = "rate-auth-login";
}
