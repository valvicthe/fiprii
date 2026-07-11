using Roblox.Rendering;
using Roblox.Website.Middleware;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Roblox;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Website.Hubs;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Formatters;

var domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(5));

var builder = WebApplication.CreateBuilder(args);

// DB Initialization Block
string pgConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(pgConnectionString) && 
    (pgConnectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
     pgConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        if (pgConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            pgConnectionString = "postgres://" + pgConnectionString.Substring(13);
        }

        var databaseUri = new Uri(pgConnectionString);
        string username = string.Empty;
        string password = string.Empty;
        
        if (!string.IsNullOrEmpty(databaseUri.UserInfo))
        {
            var userInfo = databaseUri.UserInfo.Split(':');
            username = userInfo[0];
            password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
        }

        var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port,
            Username = username,
            Password = password,
            Database = databaseUri.LocalPath.TrimStart('/'),
            Pooling = true,
            SslMode = Npgsql.SslMode.Require, 
            TrustServerCertificate = true     
        };

        pgConnectionString = connectionStringBuilder.ToString();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"CRITICAL: Failed to parse DATABASE_URL string. Details: {ex.Message}");
    }
}
else
{
    pgConnectionString = builder.Configuration.GetSection("Postgres")?.Value ?? string.Empty;
}

if (string.IsNullOrEmpty(pgConnectionString))
{
    throw new InvalidOperationException("CRITICAL: The PostgreSQL connection string could not be resolved.");
}

// Pass the cleanly structured string to your layout assembly
Roblox.Services.Database.Configure(pgConnectionString);

// ==========================================
// ROOT / ADMIN ACCOUNT AUTOMATED SEEDER
// ==========================================
try
{
    using var usersService = Roblox.Services.ServiceProvider.GetOrCreate<UsersService>();
    // Check if any accounts exist in the database yet
    var totalUsers = await usersService.GetTotalUsersCountAsync();
    if (totalUsers == 0)
    {
        Console.WriteLine("[SEED] No accounts found. Creating default root admin account...");
        // Creates the base platform account (Username, Email, Password)
        var newAdmin = await usersService.CreateUserAsync("Admin", "admin@fiprii.com", "RobloxAdmin2026!");
        
        // Elevate permissions manually via the Staff Configuration framework
        Console.WriteLine($"[SEED] Admin account provisioned with ID: {newAdmin.userId}. Elevated to Root Staff.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[WARN] Admin auto-seeding skipped or table not migrated yet: {ex.Message}");
}

// ==========================================
// DEFENSIVE SERVICE CONFIGURATION REWRITE
// ==========================================
// Safe configuration string extraction function to prevent NullReferenceExceptions
string GetSafeConfig(string key, string fallback = "") => 
    builder.Configuration.GetSection(key)?.Value ?? fallback;

Roblox.Configuration.CdnBaseUrl = GetSafeConfig("CdnBaseUrl");
Roblox.Configuration.AssetDirectory = GetSafeConfig("Directories:Asset", "/app/assets/");
Roblox.Configuration.StorageDirectory = GetSafeConfig("Directories:Storage", "/app/storage/");
Roblox.Configuration.ThumbnailsDirectory = GetSafeConfig("Directories:Thumbnails", "/app/thumbnails/");
Roblox.Configuration.GroupIconsDirectory = GetSafeConfig("Directories:GroupIcons", "/app/groupicons/");
Roblox.Configuration.PublicDirectory = GetSafeConfig("Directories:Public", "/app/wwwroot/");
Roblox.Configuration.XmlTemplatesDirectory = GetSafeConfig("Directories:XmlTemplates", "/app/templates/");
Roblox.Configuration.JsonDataDirectory = GetSafeConfig("Directories:JsonData", "/app/json/");
Roblox.Configuration.ScriptDirectory = GetSafeConfig("Directories:ScriptsData", "/app/scripts/");
Roblox.Configuration.AdminBundleDirectory = GetSafeConfig("Directories:AdminBundle", "/app/wwwroot/admin/");
Roblox.Configuration.EconomyChatBundleDirectory = GetSafeConfig("Directories:EconomyChatBundle", "/app/wwwroot/chat/");
Roblox.Configuration.BaseUrl = GetSafeConfig("BaseUrl", "https://fiprii.up.railway.app");
Roblox.Configuration.ShortBaseUrl = Roblox.Configuration.BaseUrl.Replace("https://www.", "").Replace("http://", "");
Roblox.Configuration.HCaptchaPublicKey = GetSafeConfig("HCaptcha:Public", "dummy-key");
Roblox.Configuration.HCaptchaPrivateKey = GetSafeConfig("HCaptcha:Private", "dummy-secret");

// Discord Configuration Safe Mapping
Roblox.Configuration.DiscordClientId = GetSafeConfig("Discord:ClientId", "dummy_id");
Roblox.Configuration.DiscordClientSecret = GetSafeConfig("Discord:ClientSecret", "dummy_secret");
Roblox.Configuration.DiscordGuildId = GetSafeConfig("Discord:GuildId", "dummy_guild");
Roblox.Configuration.DiscordBotToken = GetSafeConfig("Discord:BotToken", "dummy_token");
Roblox.Configuration.DiscordLogChannelId = GetSafeConfig("Discord:LogChannelId", "dummy_channel");
Roblox.Configuration.DiscordApplicationCallback = Roblox.Configuration.BaseUrl + GetSafeConfig("Discord:ApplicationCallback", "/login/callback");
Roblox.Configuration.DiscordLoginCallback = Roblox.Configuration.BaseUrl + GetSafeConfig("Discord:LoginCallback", "/login/discord");
Roblox.Configuration.DiscordLinkCallback = Roblox.Configuration.BaseUrl + GetSafeConfig("Discord:LinkCallback", "/login/link");

Roblox.Configuration.GameServerAuthorization = GetSafeConfig("GameServerAuthorization", Guid.NewGuid().ToString());
Roblox.Configuration.BotAuthorization = GetSafeConfig("BotAuthorization", Guid.NewGuid().ToString());
Roblox.Configuration.RccAuthorization = GetSafeConfig("RccAuthorization", Guid.NewGuid().ToString());
Roblox.Configuration.ArbiterAuthorization = GetSafeConfig("ArbiterAuthorization", Guid.NewGuid().ToString());
Roblox.Configuration.GameServerIp = GetSafeConfig("GameServerIp", "127.0.0.1");
Roblox.Configuration.UserAgentBypassSecret = GetSafeConfig("UserAgentBypassSecret", "bypass");
Roblox.Configuration.VerificationSecret = GetSafeConfig("VerificationSecret", "verify");
Roblox.Configuration.LuaScriptsDirectory = GetSafeConfig("Directories:RCCLuaScripts", "/app/lua/");

// Game Server Array Mapping Safeguards
try {
    IConfiguration gameServerConfig = new ConfigurationBuilder().AddJsonFile("game-servers.json", optional: true).Build();
    Roblox.Configuration.GameServerIpAddresses = gameServerConfig.GetSection("GameServers").Get<IEnumerable<GameServerConfigEntry>>() ?? new List<GameServerConfigEntry>();
} catch {
    Roblox.Configuration.GameServerIpAddresses = new List<GameServerConfigEntry>();
}

Roblox.Configuration.AssetValidationServiceUrl = GetSafeConfig("AssetValidation:BaseUrl", "http://127.0.0.1");
Roblox.Configuration.AssetValidationServiceAuthorization = GetSafeConfig("AssetValidation:Authorization", "auth");
GameServerService.Configure(string.Join(Guid.NewGuid().ToString(), new int[16].Select(_ => Guid.NewGuid().ToString())));

long.TryParse(GetSafeConfig("PackageShirtAssetId", "0"), out long shirtId);
long.TryParse(GetSafeConfig("PackagePantsAssetId", "0"), out long pantsId);
Roblox.Configuration.PackageShirtAssetId = shirtId;
Roblox.Configuration.PackagePantsAssetId = pantsId;

Roblox.Libraries.TwitterApi.TwitterApi.Configure(GetSafeConfig("Twitter:Bearer", "dummy_bearer"));

// Array extraction safety bindings
Roblox.Configuration.SignupAssetIds = builder.Configuration.GetSection("SignupAssetIds").GetChildren().Select(c => long.TryParse(c.Value, out long id) ? id : 0) ?? new List<long>();
Roblox.Configuration.SignupAvatarAssetIds = builder.Configuration.GetSection("SignupAvatarAssetIds").GetChildren().Select(c => long.TryParse(c.Value, out long id) ? id : 0) ?? new List<long>();

#if DEBUG
Roblox.Configuration.RobloxAppPrefix = "rbxeconsimdev:";
#endif

FeatureFlags.StartUpdateFlagTask();

var ownerUserIds = builder.Configuration.GetSection("OwnerUserId").Get<List<long>>() ?? new List<long> { 1 };
// Automatically include our newly seeded admin ID (1) if the config array is completely empty
if (!ownerUserIds.Contains(1)) ownerUserIds.Add(1);
Roblox.Website.Filters.StaffFilter.Configure(ownerUserIds);

// Builder Pipeline Registrations
builder.Services.AddRazorPages();
builder.Services.AddRequestDecompression();
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
    options.RespectBrowserAcceptHeader = true;
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
    c.CustomSchemaIds(type => type.FullName);
    c.EnableAnnotations();
    c.SwaggerDoc("UserV1", new OpenApiInfo { Version = "v1", Title = "Users Api v1" });
    c.SchemaGeneratorOptions.SchemaIdSelector = type => type.ToString();
    c.OperationFilter<SwaggerFileOperationFilter>();
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

builder.Services.AddMvc(c => c.Conventions.Add(new ApiExplorerGetsOnlyConvention()));

var app = builder.Build();

// Static File Routing Framework (Defensive Directory Existence Mapping)
void SafeMapStaticFiles(string physicalPath, string requestPath, Action<StaticFileResponseContext> prepareAction)
{
    if (Directory.Exists(physicalPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(physicalPath),
            RequestPath = requestPath,
            OnPrepareResponse = prepareAction,
        });
    }
}

var prepareResponseForCache = (StaticFileResponseContext ctx) =>
{
    const int durationInSeconds = 86400 * 365;
    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + durationInSeconds;
    ctx.Context.Response.Headers.Remove(HeaderNames.LastModified);
};

SafeMapStaticFiles(Path.Combine(Roblox.Configuration.PublicDirectory, "css/roblox/"), "/css", prepareResponseForCache);
SafeMapStaticFiles(Path.Combine(Roblox.Configuration.PublicDirectory, "js/"), "/js", prepareResponseForCache);
SafeMapStaticFiles(Path.Combine(Roblox.Configuration.PublicDirectory, "UnsecuredContent/"), "/UnsecuredContent", prepareResponseForCache);

if (string.IsNullOrWhiteSpace(Roblox.Configuration.CdnBaseUrl))
{
    SafeMapStaticFiles(Roblox.Configuration.ThumbnailsDirectory, "/images/thumbnails", prepareResponseForCache);
    SafeMapStaticFiles(Roblox.Configuration.GroupIconsDirectory, "/images/groups", prepareResponseForCache);
}

SafeMapStaticFiles(Path.Combine(Roblox.Configuration.PublicDirectory, "img/"), "/img", prepareResponseForCache);

// SERVE FRONTEND ADMIN PANEL ASSETS AUTOMATICALLY
// Maps fiprii.up.railway.app/admin/ cleanly to wwwroot/admin distribution bundles
app.UseDefaultFiles();
SafeMapStaticFiles(Roblox.Configuration.AdminBundleDirectory, "/admin", prepareResponseForCache);

app.UseRouting();
app.UseRobloxSessionMiddleware();
app.UseMiddleware<ThumbnailMiddleware>(Roblox.Configuration.ThumbnailsDirectory);
app.UseMiddleware<RobloxLoggingMiddleware>();
app.UseRobloxPlayerCorsMiddleware();

app.UseRobloxCsrfMiddleware();
app.UseApplicationGuardMiddleware();
Roblox.Website.Middleware.ApplicationGuardMiddleware.Configure(GetSafeConfig("Authorization", "internal-secret"));
Roblox.Website.Middleware.CsrfMiddleware.Configure(Guid.NewGuid().ToString() + Guid.NewGuid().ToString());

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.ShowCommonExtensions();
    c.SwaggerEndpoint("/swagger/UserV1/swagger.json", "UserV1");
});

app.UseMiddleware<FrontendProxyMiddleware>();
app.UseExceptionHandler("/error");

RenderingHandler.Configure();
SessionMiddleware.Configure(GetSafeConfig("Jwt:Sessions", "default_secret_key_32_bytes_long!!"));
app.UseTimerMiddleware(); 

Roblox.Services.Signer.SignService.Setup();

_ = Task.Run(async () =>
{
    try {
        using var assets = Roblox.Services.ServiceProvider.GetOrCreate<AssetsService>();
        await assets.FixAssetImagesWithoutMetadata();
    } catch (Exception ex) {
        Console.WriteLine($"[WARN] Background asset repair task skipped: {ex.Message}");
    }
});

_ = Task.Run(AvatarService.StartTimerClear3D);

app.MapControllers();
app.MapRazorPages();
app.UseWebSockets();
app.UseRequestDecompression();
app.MapHub<MessageRouterHub>("/v1/router/signalr");

app.Run();
