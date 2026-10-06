using System.Net;
using System.Net.Http.Json;

namespace Backend.Tests;

public class DeliveryTests
{
    static object Mail(Guid id, string subject="Test") => new { requestId=id,recipient="a@example.com",subject,body="Reviewed email body" };
    [Fact] public async Task FiveRepeatedRequestsInvokeDeliveryOnce()
    {
        using var app = new AppFactory(); using var owner = app.Client(); var id = Guid.NewGuid();
        for (var i=0;i<5;i++) Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync("/api/send",Mail(id))).StatusCode);
        Assert.Equal(1,app.Sender.Calls);
    }
    [Fact] public async Task ConcurrentDuplicateRequestsInvokeDeliveryOnce()
    {
        using var app = new AppFactory(); using var owner = app.Client(); await owner.GetAsync("/api/me"); var id=Guid.NewGuid();
        var replies=await Task.WhenAll(Enumerable.Range(0,5).Select(_=>owner.PostAsJsonAsync("/api/send",Mail(id))));
        Assert.All(replies,r=>Assert.Contains(r.StatusCode,new[]{HttpStatusCode.OK,HttpStatusCode.Conflict}));
        Assert.Equal(1,app.Sender.Calls);
    }
    [Fact] public async Task ReusedIdentifierWithDifferentContentIsRejected()
    {
        using var app = new AppFactory(); using var owner=app.Client(); var id=Guid.NewGuid();
        await owner.PostAsJsonAsync("/api/send",Mail(id));
        Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsJsonAsync("/api/send",Mail(id,"Changed"))).StatusCode);
        Assert.Equal(1,app.Sender.Calls);
    }
    [Theory] [InlineData("")] [InlineData("Line\nBreak")]
    public async Task InvalidSubjectDoesNotSend(string subject)
    {
        using var app = new AppFactory(); using var owner=app.Client();
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsJsonAsync("/api/send",Mail(Guid.NewGuid(),subject))).StatusCode);
        Assert.Equal(0,app.Sender.Calls);
    }
    [Fact] public async Task TimedOutDeliveryIsNotRetriedForTheSameIdentifier()
    {
        using var app = new AppFactory(); using var owner=app.Client(); var id=Guid.NewGuid(); app.Sender.Fail=true;
        Assert.Equal(HttpStatusCode.GatewayTimeout,(await owner.PostAsJsonAsync("/api/send",Mail(id))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsJsonAsync("/api/send",Mail(id))).StatusCode);
        Assert.Equal(1,app.Sender.Calls);
    }
}
