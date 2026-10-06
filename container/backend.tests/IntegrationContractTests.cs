using System.Net;
using System.Text;
using System.Text.Json;
using Backend;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests;
public sealed class FakeToken : IGoogleToken {public Task<string> TokenAsync(CancellationToken ct)=>Task.FromResult("unit-test-token");}
public sealed class ReplyHandler(Func<HttpRequestMessage,HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>Task.FromResult(reply(request));
}
public class IntegrationContractTests
{
    static AppSettings Settings => new(false,"unit-test-project","europe-west4","configured-model","https://n8n.example.test/webhook/mail","X-N8N-Token","unit-test-value","unit-test-project","academy","sample");
    static HttpResponseMessage Json(string content,HttpStatusCode code=HttpStatusCode.OK)=>new(code){Content=new StringContent(content,Encoding.UTF8,"application/json")};
    [Fact] public async Task N8nUsesHeaderAuthAndExactAcknowledgement()
    {
        var id=Guid.NewGuid(); using var client=new HttpClient(new ReplyHandler(request=>{
            Assert.Null(request.Headers.Authorization);
            Assert.Equal("unit-test-value",request.Headers.GetValues("X-N8N-Token").Single());
            var body=request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("requestedBy",body);Assert.Contains("a@example.com",body);
            return Json(JsonSerializer.Serialize(new {status="sent",requestId=id}));
        }));
        var outcome=await new N8nMailSender(client,Settings).SendAsync(new(id,"a@example.com","Test","Body"),"owner@alza.cz",default);
        Assert.Equal("acknowledged",outcome.Status);Assert.False(outcome.Simulated);
    }
    [Theory] [InlineData("{}")] [InlineData("ok")] [InlineData("{\"status\":\"sent\",\"requestId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task N8nGenericSuccessIsNotDeliveryConfirmation(string reply)
    {
        using var client=new HttpClient(new ReplyHandler(_=>Json(reply)));
        await Assert.ThrowsAsync<AppError>(()=>new N8nMailSender(client,Settings).SendAsync(new(Guid.NewGuid(),"a@example.com","Test","Body"),"owner@alza.cz",default));
    }
    [Fact] public async Task VertexUsesAdcAndValidatesStructuredResponse()
    {
        using var client=new HttpClient(new ReplyHandler(request=>{
            Assert.Equal("Bearer",request.Headers.Authorization?.Scheme);
            Assert.Contains("europe-west4-aiplatform.googleapis.com",request.RequestUri!.Host);
            var body=request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Assert.Contains("responseSchema",body);
            return Json(JsonSerializer.Serialize(new {candidates=new[]{new {finishReason="STOP",content=new {parts=new[]{new {text="{\"subject\":\"Hello\",\"body\":\"Reviewed draft\"}"}}}}}}));
        }));
        var outcome=await new VertexDraftGenerator(new GoogleHttp(client,new FakeToken()),Settings).GenerateAsync(new("a@example.com","Write a greeting","friendly","en"),default);
        Assert.Equal("Hello",outcome.Subject);Assert.False(outcome.Simulated);
    }
    [Theory] [InlineData("{\"candidates\":[]}")] [InlineData("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}")]
    public async Task BlockedOrMissingAiResponseIsNotSuccess(string reply)
    {
        using var client=new HttpClient(new ReplyHandler(_=>Json(reply)));
        await Assert.ThrowsAsync<AppError>(()=>new VertexDraftGenerator(new GoogleHttp(client,new FakeToken()),Settings).GenerateAsync(new("a@example.com","Write a greeting","friendly","en"),default));
    }
    [Fact] public async Task BigQueryOnlyReadsConfiguredTableAndCapsPreview()
    {
        var requests=new List<string>(); using var client=new HttpClient(new ReplyHandler(request=>{
            Assert.Equal(HttpMethod.Get,request.Method);requests.Add(request.RequestUri!.ToString());
            if(request.RequestUri.AbsolutePath.EndsWith("/data"))return Json(JsonSerializer.Serialize(new {rows=Enumerable.Range(0,25).Select(i=>new {f=new[]{new {v=i.ToString()}}})}));
            return Json("{\"schema\":{\"fields\":[{\"name\":\"id\"}]}}");
        }));
        var preview=await new BigQueryTestData(new GoogleHttp(client,new FakeToken()),Settings).ReadAsync(default);
        Assert.Equal(20,preview.Rows.Length);Assert.False(preview.Simulated);
        Assert.Contains(requests,url=>url.EndsWith("/data?maxResults=20"));
        Assert.All(requests,url=>Assert.Contains("/datasets/academy/tables/sample",url));
    }
    [Fact] public void CloudModeRejectsMissingConfigurationInsteadOfSimulating()
    {
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>()).Build();
        Assert.Throws<InvalidOperationException>(()=>AppSettings.Load(config));
    }
}
