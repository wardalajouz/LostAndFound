using System.Text.Json.Serialization;
using LostAndFound.Api.Auth;
using LostAndFound.Api.Data;
using LostAndFound.Api.Email;
using LostAndFound.Api.ErrorHandling;
using LostAndFound.Api.OpenApi;
using LostAndFound.Api.Services;
using LostAndFound.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddSwaggerWithJwt();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddExceptionHandler<NotImplementedExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddEmail(builder.Configuration, builder.Environment);
builder.Services.AddFileStorage(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStoredFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.SeedAdminAsync();

app.Run();

/// <summary>Exposed so integration tests can start the API in memory.</summary>
public partial class Program;
