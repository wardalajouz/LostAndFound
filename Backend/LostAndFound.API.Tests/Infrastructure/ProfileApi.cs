using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Profile;

namespace LostAndFound.Api.Tests.Infrastructure;

public static class ProfileApi
{
    public static Task<ProfileResponse> ProfileAsync(this HttpClient client, string token) =>
        client.GetAsync<ProfileResponse>("/api/users/me", token);

    public static Task<HttpResponseMessage> UpdateProfileAsync(
        this HttpClient client, string token, string email, string? currentPassword = null,
        string fullName = "Ward Alajouz", string userType = "Student") =>
        client.SendAsync(HttpMethod.Put, "/api/users/me", token, new { fullName, email, userType, currentPassword });

    public static Task<HttpResponseMessage> ChangePasswordAsync(
        this HttpClient client, string token, string currentPassword, string newPassword, string? confirm = null) =>
        client.SendAsync(HttpMethod.Put, "/api/users/me/password", token,
            new { currentPassword, newPassword, confirmNewPassword = confirm ?? newPassword });

    public static Task<UserSettings> SettingsAsync(this HttpClient client, string token) =>
        client.GetAsync<UserSettings>("/api/users/me/settings", token);

    public static Task<HttpResponseMessage> UpdateSettingsAsync(this HttpClient client, string token, object settings) =>
        client.SendAsync(HttpMethod.Put, "/api/users/me/settings", token, settings);

    public static Task<HttpResponseMessage> DeleteAccountAsync(this HttpClient client, string token, string? password) =>
        client.SendAsync(HttpMethod.Delete, "/api/users/me", token, password is null ? new { } : new { password });

    /// <summary>Registers a user and returns the token, email, and id.</summary>
    public static async Task<(string Token, string Email, Guid Id)> NewAccountAsync(this HttpClient client, string? fullName = null)
    {
        string email = ApiClientExtensions.UniqueEmail();
        string token = await client.RegisterAndGetTokenAsync(email, fullName ?? ApiClientExtensions.DefaultFullName);
        Guid id = (await client.ProfileAsync(token)).Id;
        return (token, email, id);
    }

    public static string? ResetTokenFromLastEmail(this RecordingEmailService emails, string address)
    {
        LostAndFound.Api.Email.EmailMessage? message = emails.SentTo(address).LastOrDefault();
        if (message is null)
        {
            return null;
        }

        string link = message.TextBody.Split('\n').Select(line => line.Trim()).First(line => line.StartsWith("http", StringComparison.Ordinal));
        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
    }

    public static Task<HttpResponseMessage> ForgotPasswordAsync(this HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

    public static Task<HttpResponseMessage> ResetPasswordAsync(this HttpClient client, string email, string token, string newPassword) =>
        client.PostAsJsonAsync("/api/auth/reset-password", new { email, token, newPassword, confirmNewPassword = newPassword });
}
