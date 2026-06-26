namespace BeautyStore.Api.Admin.Dtos;

public sealed record SettingsDto(
    ApplicationInfoDto Application,
    AzureStatusDto     Azure,
    SecurityInfoDto    Security,
    SystemHealthDto    System,
    DeploymentInfoDto  Deployment);

public sealed record ApplicationInfoDto(
    string   AppName,
    string   Version,
    string   Environment,
    string   ApiBaseUrl,
    DateTime BuildTimestamp);

public sealed record ServiceStatusDto(string Status, string Details = "");

public sealed record AzureStatusDto(
    ServiceStatusDto SqlDatabase,
    ServiceStatusDto BlobStorage,
    ServiceStatusDto ServiceBus,
    ServiceStatusDto ApplicationInsights,
    ServiceStatusDto OpenTelemetry);

public sealed record SecurityInfoDto(
    bool JwtEnabled,
    bool IdentityEnabled,
    bool RoleAuthorizationEnabled,
    bool ExceptionMiddlewareEnabled,
    bool ProblemDetailsEnabled);

public sealed record SystemHealthDto(
    string   HealthEndpoint,
    DateTime CurrentTimeUtc,
    string   ServerUptime,
    bool     DatabaseReachable,
    int      ProductCount,
    int      CategoryCount,
    int      UserCount);

public sealed record DeploymentInfoDto(
    string ContainerAppName,
    string ContainerRevision,
    string DeploymentEnvironment,
    string GitCommit);
