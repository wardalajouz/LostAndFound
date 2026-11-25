using System.Text.Json.Serialization;
using LostAndFound.Api.Auth;
using LostAndFound.Api.Data;
using LostAndFound.Api.Email;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.OpenApi;
using LostAndFound.Api.Security;
using LostAndFound.Api.Services;
using LostAndFound.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = SecurityServiceCollectionExtensions.MaxRequestBodyBytes);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddSwaggerWithJwt();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddEmail(builder.Configuration, builder.Environment);
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddApiSecurity(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStoredFiles();
app.UseFrontendCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

await app.SeedAdminAsync();

app.Run();

/// <summary>Exposed so integration tests can start the API in memory.</summary>
public partial class Program;
