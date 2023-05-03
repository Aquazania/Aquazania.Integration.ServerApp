using Aquazania.Telephony.Integration.Models;
using HTTPServer.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using HTTPServer.Factory.MasterPartyContract;
using HTTPServer.Factory.MasterLinkedPartyContract;
using System.Data;
using Aquazania.Integration.ServerApp.Factory;
using System.Net;
using Newtonsoft.Json;

namespace HTTPServer.Controllers
{
    public class PartyController : ApiControllerBase
    {
        [HttpPost(nameof(PostParty))]
        public async Task<IActionResult> PostParty([FromBody] List<ChangedPartyContactContract> parties)
        {
            var configuration = new ConfigurationBuilder()
                                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                    .AddJsonFile("appsettings.json", optional: true)
                                    .Build();
            int successes = 0;
            List<Error> errors = new List<Error>(); ;
            foreach (var party in parties)
            {
                List<string> Validationerrors = new List<string>();
                try
                {
                    var convertor = PartyFactory.Create(party);
                    Task<List<string>> tasks = convertor.Convert(party, configuration);
                    Validationerrors = await tasks;
                }
                catch (Exception ex)
                {
                    Validationerrors.Add(ex.Message);
                }
                if (Validationerrors.Count() == 0)
                    successes += 1;
                else
                {
                    Error error = new Error(party.PartyCode, party.PartyType, Validationerrors.ToArray());
                    errors.Add(error);
                }
            }
            var response = new Response(successes, errors.Count(), errors.ToArray());
            return Content(JsonConvert.SerializeObject(response), "application/json");
        }
        [HttpPost(nameof(PostLinkedParty))]
        public async Task<IActionResult> PostLinkedParty([FromBody] List<ChangedLinkedContactContract> parties)
        {
            var configuration = new ConfigurationBuilder()
                                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                    .AddJsonFile("appsettings.json", optional: true)
                                    .Build();
            int successes = 0;
            List<Error> errors = new List<Error>(); 
            foreach (var party in parties)
            {
                List<string> Validationerrors = new List<string>();
                try
                {
                    var convertor = LinkedPartyFactory.Create(party);
                    Task<List<string>> tasks = convertor.Convert(party, configuration);
                    Validationerrors = await tasks;
                }
                catch (Exception ex)
                {
                    Validationerrors.Add(ex.Message);
                }
                if (Validationerrors.Count() == 0)
                    successes += 1;
                else
                {
                    Error error = new Error(party.ParentPartyCode, party.ParentPartyType, Validationerrors.ToArray());
                    errors.Add(error);
                }
            }
            var response = new Response(successes, errors.Count(), errors.ToArray());
            return Content(JsonConvert.SerializeObject(response), "application/json");
        }
        [HttpGet(nameof(ConnectionCheck))]
        public string ConnectionCheck()
        {
            return "Connection Good";
        }
    }
}
