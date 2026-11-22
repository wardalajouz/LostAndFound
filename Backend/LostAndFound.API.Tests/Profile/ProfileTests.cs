using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Contracts.Posts;
using LostAndFound.Api.Contracts.Profile;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Tests.Infrastructure;

namespace LostAndFound.Api.Tests.Profile;

public sealed class ProfileTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private const string Password = ApiClientExtensions.ValidPassword;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Profile_shows_the_signed_in_users_details()
    {
        (string token, string email, Guid id) = await _client.NewAccountAsync("Ward Haddad");

        ProfileResponse profile = await _client.ProfileAsync(token);

        Assert.Equal(id, profile.Id);
        Assert.Equal("Ward Haddad", profile.FullName);
        Assert.Equal(email, profile.Email);
        Assert.Equal(UserType.Student, profile.UserType);
        Assert.Equal(UserRole.User, profile.Role);
        Assert.Null(profile.AvatarUrl);
        Assert.True(profile.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Name_and_user_type_change_without_a_password_and_show_on_posts()
    {
        (string token, string email, _) = await _client.NewAccountAsync();
        PostResponse post = await _client.CreatePostAsync(token, PostApi.Body());

        HttpResponseMessage response = await _client.UpdateProfileAsync(token, email, fullName: "  Ward H.  ", userType: "Staff");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        ProfileResponse updated = (await response.Content.ReadFromJsonAsync<ProfileResponse>(ApiClientExtensions.Json))!;
        Assert.Equal("Ward H.", updated.FullName);
        Assert.Equal(UserType.Staff, updated.UserType);
        Assert.Equal("Ward H.", (await _client.ProfileAsync(token)).FullName);
        Assert.Equal("Ward H.", (await _client.GetAsync<PostResponse>($"/api/posts/{post.Id}")).Publisher.FullName);
    }

    [Fact]
    public async Task Changing_the_email_needs_the_current_password()
    {
        (string token, string email, _) = await _client.NewAccountAsync();
        string newEmail = ApiClientExtensions.UniqueEmail();

        HttpResponseMessage missing = await _client.UpdateProfileAsync(token, newEmail);
        HttpResponseMessage wrong = await _client.UpdateProfileAsync(token, newEmail, currentPassword: "Wr0ngPassword");

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.True((await wrong.ReadJsonAsync()).GetProperty("errors").TryGetProperty("CurrentPassword", out _));
        Assert.Equal(email, (await _client.ProfileAsync(token)).Email);
    }

    [Fact]
    public async Task Changed_email_is_used_to_log_in_from_then_on()
    {
        (string token, string oldEmail, _) = await _client.NewAccountAsync();
        string newEmail = ApiClientExtensions.UniqueEmail();

        HttpResponseMessage response = await _client.UpdateProfileAsync(token, newEmail, currentPassword: Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(newEmail, (await _client.ProfileAsync(token)).Email);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(newEmail, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.LoginAsync(oldEmail, Password)).StatusCode);
    }

    [Fact]
    public async Task Changing_only_the_letter_case_needs_no_password()
    {
        (string token, string email, _) = await _client.NewAccountAsync();
        string upper = email.ToUpperInvariant();

        HttpResponseMessage response = await _client.UpdateProfileAsync(token, upper);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(upper, (await _client.ProfileAsync(token)).Email);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(email, Password)).StatusCode);
    }

    [Fact]
    public async Task Email_already_used_by_another_account_is_409()
    {
        (_, string takenEmail, _) = await _client.NewAccountAsync();
        (string token, string email, _) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.UpdateProfileAsync(token, takenEmail.ToUpperInvariant(), currentPassword: Password);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(email, (await _client.ProfileAsync(token)).Email);
    }

    [Fact]
    public async Task Changing_the_email_cancels_a_reset_link_sent_to_the_old_address()
    {
        (string token, string oldEmail, _) = await _client.NewAccountAsync();
        await _client.ForgotPasswordAsync(oldEmail);
        string resetToken = factory.Emails.ResetTokenFromLastEmail(oldEmail)!;
        string newEmail = ApiClientExtensions.UniqueEmail();

        await _client.UpdateProfileAsync(token, newEmail, currentPassword: Password);
        HttpResponseMessage reset = await _client.ResetPasswordAsync(newEmail, resetToken, "Hij4ckedPassword");

        Assert.Equal(HttpStatusCode.BadRequest, reset.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.LoginAsync(newEmail, Password)).StatusCode);
    }

    [Fact]
    public async Task Wrong_confirmation_passwords_count_toward_the_login_lockout()
    {
        (string token, string email, _) = await _client.NewAccountAsync();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await _client.UpdateProfileAsync(token, ApiClientExtensions.UniqueEmail(), currentPassword: "Wr0ngPassword");
        }

        HttpResponseMessage login = await _client.LoginAsync(email, Password);
        HttpResponseMessage rightPasswordNow = await _client.UpdateProfileAsync(token, ApiClientExtensions.UniqueEmail(), currentPassword: Password);

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rightPasswordNow.StatusCode);
        Assert.Contains("Too many", await rightPasswordNow.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("not-an-email", "Ward", "Student")]
    [InlineData("ward@findly.test", "W", "Student")]
    [InlineData("ward@findly.test", "Ward", "Alien")]
    public async Task Invalid_profile_values_return_400(string email, string fullName, string userType)
    {
        (string token, _, _) = await _client.NewAccountAsync();

        HttpResponseMessage response = await _client.UpdateProfileAsync(token, email, Password, fullName, userType);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public sealed class AdminProfileTests(AdminApiFactory factory) : IClassFixture<AdminApiFactory>
{
    [Fact]
    public async Task Admin_profile_shows_the_admin_role()
    {
        HttpClient client = factory.CreateClient();
        string token = await client.LoginAndGetTokenAsync(AdminApiFactory.AdminEmail, AdminApiFactory.AdminPassword);

        Assert.Equal(UserRole.Admin, (await client.ProfileAsync(token)).Role);
    }
}
