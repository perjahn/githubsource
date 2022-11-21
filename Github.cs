using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

class Github
{
    private readonly ILogger<Github> _logger;

    private Uri BaseAdress { get; set; } = new Uri("https://api.github.com");

    public Github()
    {
        var factory = LoggerFactory.Create(b => b.AddConsole());
        var logger = factory.CreateLogger<Github>();
        _logger = logger;
    }

    public Github(ILogger<Github> logger)
    {
        _logger = logger;
    }

    public async Task<(string text, string value)[]> GetTeams(string orgname)
    {
        using var client = new HttpClient();
        client.BaseAddress = BaseAdress;

        var address = $"orgs/{orgname}/teams";

        var githubtoken = Config.GithubToken;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubtoken);
        client.DefaultRequestHeaders.Add("User-Agent", "Fuck off");

        string content = string.Empty;
        try
        {
            var response = await client.GetAsync(address);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Get '{address}', StatusCode: {response.StatusCode}");
            }
            content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Result: >>>{content}<<<");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get '{address}'");
            _logger.LogError($"Result: >>>{content}<<<");
            _logger.LogError($"Exception: >>>{ex.ToString()}<<<");
        }

        if (!TryParseJArray(content, out JArray jarray))
        {
            _logger.LogError($"Couldn't parse result: >>>{content}<<<");
            return Array.Empty<(string, string)>();
        }

        var teams = new List<(string text, string value)>();

        foreach (var team in jarray)
        {
            if (team != null && team is JObject jobject)
            {
                var name = jobject["name"];
                var slug = jobject["slug"];
                if (name != null && name is JValue jvalueName && slug != null && slug is JValue jvalueSlug)
                {
                    var valueName = jvalueName.Value<string>();
                    var valueSlug = jvalueSlug.Value<string>();
                    if (valueName != null && valueSlug != null)
                    {
                        teams.Add((valueName, valueSlug));
                    }
                }
            }
        }

        return teams.ToArray();
    }

    async Task<(string name, string repourl)[]> GetTeamRepos(HttpClient client, string orgname, string teamname)
    {
        var address = $"orgs/{orgname}/teams/{teamname}/repos";

        string content = string.Empty;
        try
        {
            var response = await client.GetAsync(address);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Get '{address}', StatusCode: {response.StatusCode}");
            }
            content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Result: >>>{content}<<<");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get '{address}'");
            _logger.LogError($"Result: >>>{content}<<<");
            _logger.LogError($"Exception: >>>{ex.ToString()}<<<");
        }

        if (!TryParseJArray(content, out JArray jarray))
        {
            _logger.LogError($"Couldn't parse result: >>>{content}<<<");
            return Array.Empty<(string, string)>();
        }

        var repourls = new List<(string name, string repourl)>();

        foreach (var repo in jarray)
        {
            if (repo != null && repo is JObject jobject)
            {
                var name = jobject["name"];
                var url = jobject["url"];
                if (name != null && name is JValue jvalueName && url != null && url is JValue jvalueUrl)
                {
                    var valueName = jvalueName.Value<string>();
                    var valueUrl = jvalueUrl.Value<string>();
                    if (valueName != null && valueUrl != null)
                    {
                        repourls.Add((valueName, valueUrl));
                    }
                }
            }
        }

        return repourls.ToArray();
    }

    async Task<JArray> GetPRs(HttpClient client, string orgname)
    {
        var address = $"https://api.github.com/search/issues?q=org:{orgname}+is:pr+state:open";

        string content = string.Empty;
        try
        {
            var response = await client.GetAsync(address);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Get '{address}', StatusCode: {response.StatusCode}");
            }
            content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Result: >>>{content}<<<");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Get '{address}'");
            _logger.LogError($"Result: >>>{content}<<<");
            _logger.LogError($"Exception: >>>{ex.ToString()}<<<");
        }

        if (!TryParseJObject(content, out JObject jobject))
        {
            _logger.LogError($"Couldn't parse result: >>>{content}<<<");
            return new JArray();
        }

        var items = jobject["items"];
        if (items != null && items is JArray jarray)
        {
            return jarray;
        }

        return new JArray();
    }

    public async Task<(string[] columns, string[][] rows)> GetTeamsPRs(string orgname, string[] teamnames)
    {
        using var client = new HttpClient();
        client.BaseAddress = BaseAdress;

        var githubtoken = Config.GithubToken;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubtoken);
        client.DefaultRequestHeaders.Add("User-Agent", "Fuck off");

        var tasks = new List<Task<(string name, string repourl)[]>>();
        foreach (var teamname in teamnames)
        {
            tasks.Add(GetTeamRepos(client, orgname, teamname));
        }
        var repourls = new List<(string name, string repourl)>();
        repourls.AddRange((await Task.WhenAll(tasks)).SelectMany(x => x));

        var prs = await GetPRs(client, orgname);

        var columns = new[] { "reponame", "number", "title", "user.login", "created_at", "body", "pull_request.html_url" };

        var rows = new List<string[]>();

        foreach (var pr in prs)
        {
            if (pr != null && pr is JObject jobject)
            {
                var repoMatches = repourls.Where(r => jobject["repository_url"]?.Value<string>() == r.repourl).ToArray();
                if (repoMatches.Length >= 1)
                {
                    var row = new string[columns.Length];
                    row[0] = repoMatches[0].name;

                    foreach (var prop in jobject)
                    {
                        var colname = prop.Key;

                        var includeColumn = columns.Where(ic => ic == colname || ic.StartsWith($"{colname}.")).ToArray();
                        if (includeColumn.Length != 1)
                        {
                            continue;
                        }

                        colname = includeColumn[0];

                        int col = 0;
                        for (col = 0; col < columns.Length; col++)
                        {
                            if (columns[col] == colname)
                            {
                                break;
                            }
                        }

                        if (prop.Value?.Type == JTokenType.Null)
                        {
                            row[col] = string.Empty;
                        }
                        else if (prop.Value?.Type == JTokenType.String)
                        {
                            row[col] = prop.Value?.Value<string>() ?? string.Empty;
                        }
                        else if (prop.Value?.Type == JTokenType.Boolean)
                        {
                            row[col] = prop.Value.Value<bool>().ToString();
                        }
                        else if (prop.Value?.Type == JTokenType.Date)
                        {
                            row[col] = prop.Value.Value<DateTime>().ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else if (prop.Value?.Type == JTokenType.Integer)
                        {
                            row[col] = prop.Value.Value<long>().ToString();
                        }
                        else if (prop.Value?.Type == JTokenType.Float)
                        {
                            row[col] = prop.Value.Value<double>().ToString();
                        }
                        else if (prop.Value?.Type == JTokenType.Object && colname.Contains('.'))
                        {
                            var jobject2 = prop.Value.Value<JObject>();
                            if (jobject2 != null)
                            {
                                var jproperty2 = jobject2[colname.Substring(colname.IndexOf('.') + 1)];
                                if (jproperty2 != null)
                                {
                                    row[col] = jproperty2.Value<string>() ?? string.Empty;
                                }
                            }
                        }
                        else
                        {
                            row[col] = $"[[{prop.Value?.Type.ToString()}]]";
                        }
                    }
                    rows.Add(row);
                }
            }
        }

        return (columns: columns.ToArray(), rows: rows.ToArray());
    }

    bool TryParseJArray(string json, out JArray jarray)
    {
        try
        {
            jarray = JArray.Parse(json);
            return true;
        }
        catch
        {
            jarray = new JArray();
            return false;
        }
    }

    bool TryParseJObject(string json, out JObject jobject)
    {
        try
        {
            jobject = JObject.Parse(json);
            return true;
        }
        catch
        {
            jobject = new JObject();
            return false;
        }
    }
}
