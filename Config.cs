using System.IO;
using Newtonsoft.Json.Linq;

class Config
{
    public static string GithubToken
    {
        get
        {
            var devfilename = "appsettings.Development.json";

            var filename = File.Exists(devfilename) ? devfilename : "appsettings.json";
            var content = File.ReadAllText(filename);
            var jobject = JObject.Parse(content);

            return jobject["githubtoken"]?.Value<string>() ?? string.Empty;
        }
    }

    public static string OrgName
    {
        get
        {
            var devfilename = "appsettings.Development.json";

            var filename = File.Exists(devfilename) ? devfilename : "appsettings.json";
            var content = File.ReadAllText(filename);
            var jobject = JObject.Parse(content);

            return jobject["orgname"]?.Value<string>() ?? string.Empty;
        }
    }
}
