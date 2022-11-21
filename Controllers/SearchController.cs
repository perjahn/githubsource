using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace k8ssource.Controllers
{
    public class Map
    {
        public string text { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }

    public class SearchRequest
    {
        public string target { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("search")]
    [Produces("application/json")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;

        public SearchController(ILogger<SearchController> logger)
        {
            _logger = logger;
        }

        // POST /search
        [HttpPost]
        public async Task<IEnumerable<Map>> Post([FromBody] SearchRequest value)
        {
            _logger.LogDebug($"{DateTime.UtcNow:HH:mm:ss}: search");
            _logger.LogDebug($"{DateTime.UtcNow:HH:mm:ss}: {value.target}");

            var github = new Github();
            var orgname = Config.OrgName;
            var teams = await github.GetTeams(orgname);

            return teams.Select(t => new Map() { text = t.text, value = t.value }).ToArray();
        }
    }
}
