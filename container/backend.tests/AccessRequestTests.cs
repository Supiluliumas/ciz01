using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
namespace Backend.Tests;

public class AccessRequestTests
{
    static async Task Bootstrap(HttpClient owner) => Assert.Equal(HttpStatusCode.OK,(await owner.GetAsync("/api/me")).StatusCode);
    static async Task<JsonElement> Status(HttpClient client) => await client.GetFromJsonAsync<JsonElement>("/api/access-request");
    static Task<HttpResponseMessage> Apply(HttpClient client) => client.PostAsJsonAsync("/api/access-request", new {email="forged@alza.cz"});
    static Task<HttpResponseMessage> Decide(HttpClient owner,string email,string decision) => owner.PostAsJsonAsync("/api/admin/requests",new {email,decision});

    [Theory]
    [InlineData(null)] [InlineData("outsider@example.com")] [InlineData("person@alza.cz.attacker.test")]
    [InlineData("person@sub.alza.cz")] [InlineData("person/other@alza.cz")]
    public async Task RequestsRequireExactCorporateIdentity(string? email)
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var client=app.Client(email);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/access-request")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await Apply(client)).StatusCode);
    }
    [Fact] public async Task SubmissionUsesOnlyOwnIdentityAndIsIdempotent()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var user=app.Client("Applicant@Alza.sk");
        Assert.Equal("none",(await Status(user)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK,(await Apply(user)).StatusCode);
        await Apply(user);
        var own=await Status(user);
        Assert.Equal("applicant@alza.sk",own.GetProperty("email").GetString());
        Assert.Equal("pending",own.GetProperty("status").GetString());
        Assert.False(own.GetProperty("allowed").GetBoolean());
        var requests=await owner.GetFromJsonAsync<JsonElement>("/api/admin/requests");
        Assert.Equal(1,requests.GetArrayLength());
        Assert.Equal("applicant@alza.sk",requests[0].GetProperty("email").GetString());
        Assert.Equal(HttpStatusCode.Forbidden,(await user.GetAsync("/api/admin/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await Decide(user,"applicant@alza.sk","approve")).StatusCode);
    }
    [Fact] public async Task PendingUserCannotUseAnyBusinessFeatureOrExemptionPrefix()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var user=app.Client("waiting@alza.cz");await Apply(user);
        foreach(var path in new[]{"/api/me","/api/status","/api/test-data","/api/admin/access","/api/access-request/other"})
            Assert.Equal(HttpStatusCode.Forbidden,(await user.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await user.PostAsJsonAsync("/api/drafts",new {recipient="a@example.com",instructions="Write a greeting please",tone="friendly",language="cs"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await user.PostAsJsonAsync("/api/send",new {requestId=Guid.NewGuid(),recipient="a@example.com",subject="Test",body="Body"})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await user.PutAsJsonAsync("/api/access-request",new{})).StatusCode);
        Assert.Equal(0,app.Sender.Calls);
    }
    [Fact] public async Task ApprovalGrantsOneOrdinaryUserAndRevocationAllowsReapplication()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var user=app.Client("approved@alza.cz");using var other=app.Client("other@alza.cz");await Apply(user);await Apply(other);
        Assert.Equal(HttpStatusCode.OK,(await Decide(owner,"APPROVED@ALZA.CZ","approve")).StatusCode);
        var own=await Status(user);Assert.True(own.GetProperty("allowed").GetBoolean());Assert.False(own.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(HttpStatusCode.OK,(await user.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await user.GetAsync("/api/admin/requests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await other.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await Decide(owner,"approved@alza.cz","reject")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await owner.DeleteAsync("/api/admin/members?email=approved%40alza.cz&role=user")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await user.GetAsync("/api/me")).StatusCode);
        Assert.Equal("revoked",(await Status(user)).GetProperty("status").GetString());
        await Apply(user);Assert.Equal("pending",(await Status(user)).GetProperty("status").GetString());
    }
    [Fact] public async Task RejectionStaysDeniedAndDoesNotResubmitAutomatically()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var user=app.Client("rejected@alza.cz");await Apply(user);
        Assert.Equal(HttpStatusCode.OK,(await Decide(owner,"rejected@alza.cz","reject")).StatusCode);
        await Apply(user);Assert.Equal("rejected",(await Status(user)).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Forbidden,(await user.GetAsync("/api/me")).StatusCode);
        Assert.Equal(0,(await owner.GetFromJsonAsync<JsonElement>("/api/admin/requests")).GetArrayLength());
    }
    [Fact] public async Task ConcurrentReviewHasOneWinner()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var user=app.Client("race@alza.cz");await Apply(user);
        var responses=await Task.WhenAll(Decide(owner,"race@alza.cz","approve"),Decide(owner,"race@alza.cz","reject"));
        Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.OK);
        Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Conflict);
    }
    [Fact] public async Task RequestMutationRequiresAppHeaderAndSameOrigin()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        using var user=app.Client("waiting@alza.cz");user.DefaultRequestHeaders.Remove("X-App-Request");
        Assert.Equal(HttpStatusCode.Forbidden,(await Apply(user)).StatusCode);
        user.DefaultRequestHeaders.Add("X-App-Request","1");user.DefaultRequestHeaders.Add("Origin","https://attacker.test");
        Assert.Equal(HttpStatusCode.Forbidden,(await Apply(user)).StatusCode);
    }
    [Fact] public async Task ApprovedUsersCannotRequestForSomeoneElseAndExternalMembersCannotBeAdded()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        await Apply(owner);
        Assert.Equal(0,(await owner.GetFromJsonAsync<JsonElement>("/api/admin/requests")).GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsJsonAsync("/api/admin/members",new {email="external@example.com",role="user"})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await Decide(owner,"owner@alza.cz","unexpected")).StatusCode);
    }
    [Fact] public async Task RevokedAdministratorIsRecheckedInsideStoreReadsAndDecisions()
    {
        using var app=new AppFactory();using var owner=app.Client();await Bootstrap(owner);
        await owner.PostAsJsonAsync("/api/admin/members",new {email="reviewer@alza.cz",role="admin"});
        using var applicant=app.Client("requester@alza.cz");await Apply(applicant);
        await owner.DeleteAsync("/api/admin/members?email=reviewer%40alza.cz&role=admin");
        var store=Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Backend.Access.IAppStore>(app.Services);
        Assert.Equal(403,(await Assert.ThrowsAsync<Backend.Models.AppError>(()=>store.RequestsAsync("reviewer@alza.cz",default))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Backend.Models.AppError>(()=>store.StateAsync("reviewer@alza.cz",default))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<Backend.Models.AppError>(()=>store.ReviewAsync("reviewer@alza.cz","requester@alza.cz",true,default))).Status);
        Assert.False((await Status(applicant)).GetProperty("allowed").GetBoolean());
    }

}
