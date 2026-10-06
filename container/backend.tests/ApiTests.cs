using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests;

public class AppFactory : WebApplicationFactory<Program>
{
    public AppFactory() => Environment.SetEnvironmentVariable("VIBE_LOCAL", "1");
    public CountingSender Sender { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["VIBE_LOCAL"] = "1" }));
        builder.ConfigureServices(services => services.AddSingleton<IMailSender>(Sender));
    }
    public HttpClient Client(string? email = "owner@alza.cz")
    {
        var client = CreateClient();
        if (email != null) client.DefaultRequestHeaders.Add("X-Goog-Authenticated-User-Email", "accounts.google.com:" + email);
        client.DefaultRequestHeaders.Add("X-App-Request", "1");
        return client;
    }
}

public sealed class CountingSender : IMailSender
{
    public int Calls;
    public bool Fail { get; set; }
    public Task<DeliveryOutcome> SendAsync(MailSubmission mail, string actor, CancellationToken ct)
    {
        Interlocked.Increment(ref Calls);
        if (Fail) return Task.FromException<DeliveryOutcome>(new OperationCanceledException());
        return Task.FromResult(new DeliveryOutcome("simulated", true));
    }
}

public class ApiTests
{
    [Fact] public async Task HealthIsPublicButApiRequiresIdentity()
    {
        using var app = new AppFactory(); using var client = app.Client(null);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/healthz")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }
    [Fact] public async Task FirstVisitorIsAdminAndUnlistedUserIsDenied()
    {
        using var app = new AppFactory(); using var owner = app.Client();
        var me = await owner.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.True(me.GetProperty("isAdmin").GetBoolean());
        using var stranger = app.Client("stranger@alza.cz");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/api/me")).StatusCode);
    }
    [Fact] public async Task ConcurrentBootstrapCreatesOneAdmin()
    {
        using var app = new AppFactory(); using var one = app.Client("one@alza.cz"); using var two = app.Client("two@alza.cz");
        var responses = await Task.WhenAll(one.GetAsync("/api/me"), two.GetAsync("/api/me"));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Forbidden);
    }
    [Fact] public async Task AdminCanAdmitUserButUserCannotManageMembers()
    {
        using var app = new AppFactory(); using var owner = app.Client(); await owner.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync("/api/admin/members", new { email="User@Alza.cz", role="user" })).StatusCode);
        using var user = app.Client("USER@alza.cz");
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/admin/access")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsJsonAsync("/api/admin/members", new { email="other@alza.cz", role="admin" })).StatusCode);
    }
    [Fact] public async Task LastAdminCannotBeRemoved()
    {
        using var app = new AppFactory(); using var owner = app.Client(); await owner.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync("/api/admin/members?email=owner%40alza.cz&role=admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync("/api/admin/access")).StatusCode);
    }
    [Fact] public async Task RemovedAdminLosesAdministration()
    {
        using var app = new AppFactory(); using var owner = app.Client(); await owner.GetAsync("/api/me");
        await owner.PostAsJsonAsync("/api/admin/members", new { email="other@alza.cz", role="admin" });
        using var other = app.Client("other@alza.cz");
        Assert.Equal(HttpStatusCode.OK, (await other.DeleteAsync("/api/admin/members?email=owner%40alza.cz&role=admin")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.GetAsync("/api/admin/access")).StatusCode);
    }
    [Fact] public async Task ModeIsRestrictedByDefaultAndExplicitlyChangeable()
    {
        using var app = new AppFactory(); using var owner = app.Client(); await owner.GetAsync("/api/me");
        var access = await owner.GetFromJsonAsync<JsonElement>("/api/admin/access");
        Assert.Equal("allowlist", access.GetProperty("mode").GetString());
        await owner.PutAsJsonAsync("/api/admin/mode", new { mode="everyone" });
        using var user = app.Client("other@alza.cz");
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/api/admin/access")).StatusCode);
    }
    [Theory]
    [InlineData("bad", "Valid instructions here", "professional", "cs")]
    [InlineData("a@localhost", "Valid instructions here", "professional", "cs")]
    [InlineData("a@example.com", "short", "professional", "cs")]
    [InlineData("a@example.com", "Valid instructions here", "invalid", "cs")]
    [InlineData("a@example.com", "Valid instructions here", "professional", "xx")]
    public async Task InvalidDraftIsRejected(string recipient, string instructions, string tone, string language)
    {
        using var app = new AppFactory(); using var owner = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/drafts", new {recipient,instructions,tone,language})).StatusCode);
        Assert.Equal(0, app.Sender.Calls);
    }
    [Fact] public async Task DraftingNeverSendsAndLabelsSimulation()
    {
        using var app = new AppFactory(); using var owner = app.Client();
        var response = await owner.PostAsJsonAsync("/api/drafts", new { recipient="a@example.com", instructions="Požádej kolegu o termín schůzky.", tone="friendly", language="cs" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var draft = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(draft.GetProperty("simulated").GetBoolean());
        Assert.NotEmpty(draft.GetProperty("body").GetString()!);
        Assert.Equal(0, app.Sender.Calls);
    }
    [Fact] public async Task MutationRequiresAppHeader()
    {
        using var app = new AppFactory(); using var owner = app.Client(); owner.DefaultRequestHeaders.Remove("X-App-Request");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/drafts", new { recipient="a@example.com", instructions="Valid instructions here", tone="friendly", language="cs" })).StatusCode);
    }
    [Fact] public async Task UnknownApiReturns404InsteadOfHtml()
    {
        using var app = new AppFactory(); using var owner = app.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync("/api/not-a-route")).StatusCode);
    }
    [Theory]
    [InlineData("/api/not-a-route")]
    [InlineData("/api/admin/not-a-route")]
    public async Task UnknownApiStillRequiresIdentityAndAccess(string path)
    {
        using var app = new AppFactory(); using var anonymous = app.Client(null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(path)).StatusCode);
        using var owner = app.Client(); await owner.GetAsync("/api/me");
        using var stranger = app.Client("stranger@alza.cz");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(path)).StatusCode);
    }
    [Fact] public async Task CrossOriginMutationIsRejectedEvenWithIdentity()
    {
        using var app = new AppFactory(); using var owner = app.Client();
        owner.DefaultRequestHeaders.Add("Origin", "https://unrelated.example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PostAsJsonAsync("/api/drafts", new { recipient="a@example.com", instructions="Valid instructions here", tone="friendly", language="cs" })).StatusCode);
    }
    [Fact] public async Task TestPreviewIsBoundedAndMarkedAsSimulation()
    {
        using var app = new AppFactory(); using var owner = app.Client();
        var preview = await owner.GetFromJsonAsync<JsonElement>("/api/test-data");
        Assert.True(preview.GetProperty("simulated").GetBoolean());
        Assert.InRange(preview.GetProperty("rows").GetArrayLength(),1,20);
    }
}
