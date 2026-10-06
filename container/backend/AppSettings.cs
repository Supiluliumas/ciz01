using Backend.Models;
namespace Backend;
public sealed record AppSettings(bool Local, string Project, string Region, string Model,
    string WebhookUrl, string HeaderName, string AuthToken, string BigQueryProject,
    string BigQueryDataset, string BigQueryTable)
{
    public static AppSettings Load(IConfiguration config)
    {
        string Get(string key) => config[key] ?? "";
        var settings = new AppSettings(Get("VIBE_LOCAL")=="1",Get("GCP_PROJECT_ID"),
            Get("VERTEX_LOCATION") is {Length:>0} location ? location : "europe-west4",
            Get("VERTEX_MODEL"),Get("N8N_WEBHOOK_URL"),
            Get("N8N_HEADER_NAME") is {Length:>0} header ? header : "X-N8N-Token",
            Get("N8N_AUTH_TOKEN"),Get("BIGQUERY_PROJECT_ID"),Get("BIGQUERY_DATASET"),Get("BIGQUERY_TABLE"));
        if (!settings.Local)
        {
            foreach (var key in new[]{"GCP_PROJECT_ID","VERTEX_MODEL","N8N_WEBHOOK_URL","N8N_AUTH_TOKEN","BIGQUERY_PROJECT_ID","BIGQUERY_DATASET","BIGQUERY_TABLE"})
                if(string.IsNullOrWhiteSpace(Get(key))) throw new InvalidOperationException($"Missing required configuration: {key}");
            if (!Uri.TryCreate(settings.WebhookUrl,UriKind.Absolute,out var uri) || uri.Scheme!="https" || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidOperationException("N8N_WEBHOOK_URL must be an HTTPS URL without embedded credentials.");
            foreach(var value in new[]{settings.Region,settings.Model,settings.Project,settings.BigQueryProject,settings.BigQueryDataset,settings.BigQueryTable})
                if(!System.Text.RegularExpressions.Regex.IsMatch(value,@"^[a-zA-Z0-9_-]+$")) throw new InvalidOperationException("Invalid cloud resource identifier.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(settings.HeaderName,@"^[A-Za-z0-9-]+$") ||
                new[]{"authorization","cookie","host","content-type","content-length"}.Contains(settings.HeaderName.ToLowerInvariant()))
                throw new InvalidOperationException("N8N_HEADER_NAME must be a custom HTTP header, for example X-N8N-Token.");
            if(settings.AuthToken.Contains('\r') || settings.AuthToken.Contains('\n')) throw new InvalidOperationException("Invalid webhook authentication token.");
        }
        return settings;
    }
}
public static class ConfigEnvLoader
{
    public static void Load(ConfigurationManager config)
    {
        var path = new[]{"config.env","../config.env"}.FirstOrDefault(File.Exists);
        if(path is null) return;
        foreach(var raw in File.ReadAllLines(path))
        {
            var line=raw.Trim(); if(line.StartsWith('#') || !line.Contains('=')) continue;
            var parts=line.Split('=',2); var key=parts[0].Trim();
            if(config[key] is null) config[key]=parts[1].Trim();
        }
    }
}
