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

public class BoundaryTests
{
    [Theory] [InlineData(201,1)] [InlineData(1,10001)]
    public async Task OversizedEmailIsRejectedBeforeDelivery(int subjectLength,int bodyLength)
    {
        using var app=new AppFactory();using var client=app.Client();
        var reply=await client.PostAsJsonAsync("/api/send",new {requestId=Guid.NewGuid(),recipient="a@example.com",subject=new string('a',subjectLength),body=new string('a',bodyLength)});
        Assert.Equal(HttpStatusCode.BadRequest,reply.StatusCode);Assert.Equal(0,app.Sender.Calls);
    }
    [Fact] public async Task MaximumUnicodeEmailIsAccepted()
    {
        using var app=new AppFactory();using var client=app.Client();
        var reply=await client.PostAsJsonAsync("/api/send",new {requestId=Guid.NewGuid(),recipient="a@example.com",subject=new string('ž',200),body=new string('漢',10000)});
        Assert.Equal(HttpStatusCode.OK,reply.StatusCode);Assert.Equal(1,app.Sender.Calls);
    }
    [Fact] public async Task AnotherAllowedUserCannotReuseSendIdentifier()
    {
        using var app=new AppFactory();using var owner=app.Client();var id=Guid.NewGuid();
        await owner.PostAsJsonAsync("/api/send",new {requestId=id,recipient="a@example.com",subject="Test",body="Body"});
        await owner.PostAsJsonAsync("/api/admin/members",new {email="other@alza.cz",role="user"});
        using var other=app.Client("other@alza.cz");
        Assert.Equal(HttpStatusCode.Conflict,(await other.PostAsJsonAsync("/api/send",new {requestId=id,recipient="a@example.com",subject="Test",body="Body"})).StatusCode);
        Assert.Equal(1,app.Sender.Calls);
    }
}
