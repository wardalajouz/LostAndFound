using System.Net;
using LostAndFound.Api.Contracts.Users;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LostAndFound.Api.Tests.Auth;

/// <summary>A valid, unexpired token stops working as soon as its account is disabled or deleted.</summary>
public sealed class ActiveAccountTests(FindlyApiFactory factory) : IClassFixture<FindlyApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Disabling_an_account_cuts_off_its_existing_token()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);

        await SetAsync(id, status: AccountStatus.Disabled);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(HttpMethod.Post, "/api/posts", token, PostApi.Body())).StatusCode);
    }

    [Fact]
    public async Task Reactivating_makes_the_same_token_work_again()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();
        await SetAsync(id, status: AccountStatus.Disabled);

        await SetAsync(id, status: AccountStatus.Active);

        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(HttpMethod.Get, "/api/users/me", token)).StatusCode);
    }

    [Fact]
    public async Task Token_of_a_deleted_account_is_rejected()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();

        await SetAsync(id, deleted: true);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(HttpMethod.Get, "/api/users/me/posts", token)).StatusCode);
    }

    [Fact]
    public async Task Rejected_token_on_a_public_endpoint_is_treated_as_a_guest()
    {
        (string token, _, Guid id) = await _client.NewAccountAsync();
        await SetAsync(id, status: AccountStatus.Disabled);

        HttpResponseMessage response = await _client.SendAsync(HttpMethod.Get, "/api/campus-locations", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task SetAsync(Guid userId, AccountStatus? status = null, bool deleted = false) =>
        factory.WithDbAsync(async db =>
        {
            IQueryable<LostAndFound.Api.Data.Entities.User> user = db.Users.Where(u => u.Id == userId);
            if (status is { } newStatus)
            {
                await user.ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, newStatus));
            }

            if (deleted)
            {
                await user.ExecuteUpdateAsync(s => s.SetProperty(u => u.DeletedAt, DateTimeOffset.UtcNow));
            }
        });
}
