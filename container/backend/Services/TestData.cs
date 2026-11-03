using System.Text.Json;
using Backend.Models;
namespace Backend.Services;
public interface ITestDataReader {Task<TestPreview> ReadAsync(CancellationToken ct);}
public sealed class DemoTestData : ITestDataReader
{
    public Task<TestPreview> ReadAsync(CancellationToken ct) => Task.FromResult(new TestPreview(
        ["id","nazev","kategorie"],[["1","Ukázkový projekt","AI Academy"],["2","Testovací e-mail","Komunikace"],["3","Vibe coding","Vzdělávání"]],true,"Ukázková data v paměti"));
}
public sealed class BigQueryTestData(GoogleHttp google,AppSettings settings) : ITestDataReader
{
    public async Task<TestPreview> ReadAsync(CancellationToken ct)
    {
        if(!settings.TestDataConfigured) throw new AppError(503,"Náhled testovacích dat zatím není nastavený.");
        var path=$"https://bigquery.googleapis.com/bigquery/v2/projects/{settings.BigQueryProject}/datasets/{settings.BigQueryDataset}/tables/{settings.BigQueryTable}";
        using var schema=await google.JsonAsync(HttpMethod.Get,path,null,ct);
        using var content=await google.JsonAsync(HttpMethod.Get,path+"/data?maxResults=20",null,ct);
        try
        {
            var columns=schema.RootElement.GetProperty("schema").GetProperty("fields").EnumerateArray().Select(f=>f.GetProperty("name").GetString()!).ToArray();
            var rows=content.RootElement.TryGetProperty("rows",out var values)?values.EnumerateArray().Take(20).Select(row=>
                row.GetProperty("f").EnumerateArray().Select(cell=>{
                    var value=cell.GetProperty("v");return value.ValueKind==JsonValueKind.Null?"":value.ValueKind==JsonValueKind.String?value.GetString()!:value.GetRawText();
                }).ToArray()).ToArray():[];
            return new TestPreview(columns,rows,false,$"{settings.BigQueryProject}.{settings.BigQueryDataset}.{settings.BigQueryTable}");
        }
        catch(Exception exception) when(exception is KeyNotFoundException or InvalidOperationException)
        {throw new AppError(502,"Testovací tabulka vrátila neočekávaný formát.");}
    }
}
