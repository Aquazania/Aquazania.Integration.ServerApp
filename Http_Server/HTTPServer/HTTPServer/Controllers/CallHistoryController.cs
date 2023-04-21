using Aquazania.Integration.ServerApp.Factory;
using Aquazania.Integration.ServerApp.PostCallHistoryEntryContract;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Controllers.Base;
using HTTPServer.Factory.MasterPartyContract;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Aquazania.Integration.ServerApp.Controllers
{
    public class CallHistoryController : ApiControllerBase
    {
        [HttpPost(nameof(PostCallHistoryEntry))]
        public async Task<IActionResult> PostCallHistoryEntry([FromBody] List<CallHistoryEntryContract> callHistories)
        {
            int successes = 0;
            List<Error> errors = new List<Error>(); ;
            foreach (var callResult in callHistories)
            {
                List<string> Validationerrors = new List<string>();
                try
                {
                    CallHistoryEntry callHistory = new CallHistoryEntry();
                    Task<List<string>> tasks = callHistory.RecordHistory(callResult);
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
                    Error error = new Error(callResult.PartyCode, callResult.PartyType, Validationerrors.ToArray());
                    errors.Add(error);
                }
            }
            var response = new Response(successes, errors.Count(), errors.ToArray());
            return Content(JsonConvert.SerializeObject(response), "application/json");
        }
    }
}
