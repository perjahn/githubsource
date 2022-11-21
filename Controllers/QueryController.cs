using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace k8ssource.Controllers
{
    public class QueryRequest
    {
        public int panelId { get; set; } = 0;
        public QueryRange range { get; set; } = new QueryRange();
        public QueryRaw rangeRaw { get; set; } = new QueryRaw();
        public string interval { get; set; } = string.Empty;
        public int intervalMs { get; set; } = 0;
        public QueryTarget[] targets { get; set; } = new QueryTarget[] { };
        public QueryAdhocFilter[] adhocFilters { get; set; } = new QueryAdhocFilter[] { };
        public string format { get; set; } = string.Empty;
        public int maxDataPoints { get; set; } = 0;
    }

    public class QueryRange
    {
        public DateTime from { get; set; } = DateTime.MinValue;
        public DateTime to { get; set; } = DateTime.MinValue;
        public QueryRaw raw { get; set; } = new QueryRaw();
    }

    public class QueryRaw
    {
        public string from { get; set; } = string.Empty;
        public string to { get; set; } = string.Empty;
    }

    public class QueryTarget
    {
        public string target { get; set; } = string.Empty;
        public string refId { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    public class QueryAdhocFilter
    {
        public string key { get; set; } = string.Empty;
        [BindProperty(Name = "operator")]
        public string operatorx { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }


    public class QueryResult
    {
        public QueryColumns[] columns { get; set; } = Array.Empty<QueryColumns>();
        public string[][] rows { get; set; } = new[] { Array.Empty<string>() };
        public string type { get; set; } = "table";
    }

    public class QueryColumns
    {
        public string text { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("query")]
    [Produces("application/json")]
    public class QueryController : ControllerBase
    {
        private readonly ILogger<QueryController> _logger;

        public QueryController(ILogger<QueryController> logger)
        {
            _logger = logger;
        }

        // POST /query
        [HttpPost]
        public async Task<IEnumerable<QueryResult>> Post([FromBody] QueryRequest value)
        {
            _logger.LogDebug($"{DateTime.UtcNow:HH:mm:ss}: query");
            _logger.LogDebug($"{DateTime.UtcNow:HH:mm:ss}: query: {value.ToString()}");

            var teamnames = value.targets.SelectMany(t =>
                (t.target.StartsWith('(') && t.target.EndsWith(')') ? t.target.Substring(1, t.target.Length - 2) : t.target).Split('|')
                ).ToArray();

            var github = new Github();
            var orgname = Config.OrgName;
            var teamprs = await github.GetTeamsPRs(orgname, teamnames);

            var result = new QueryResult();

            result.columns = teamprs.columns.Select(c => new QueryColumns() { text = c, type = "string" }).ToArray();
            result.rows = teamprs.rows;

            return new[] { result };
        }
    }
}
