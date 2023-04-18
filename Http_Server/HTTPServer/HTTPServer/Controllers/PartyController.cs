using Aquazania.Telephony.Integration.Models;
using HTTPServer.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HTTPServer.Factory.MasterPartyContract;
using HTTPServer.Factory.MasterLinkedPartyContract;
using Aquazania.Integration.ServerApp.PostCallHistoryEntryContract;

namespace HTTPServer.Controllers
{
    public class PartyController : ApiControllerBase
    {
        [HttpPost(nameof(PostParty))]
        public IActionResult PostParty([FromBody] List<ChangedPartyContactContract> parties)
        {
            int rows = 0;
            foreach (var party in parties)
            {
                var convertor = PartyFactory.Create(party);
                rows = convertor.Convert(party);
            }
            return Content("Successfully recieved.", "application/json");
        }

        [HttpPost(nameof(PostLinkedParty))]
        public IActionResult PostLinkedParty([FromBody] List<ChangedLinkedContactContract> parties)
        {
            int rows = 0;
            foreach (var party in parties)
            {
                var convertor = LinkedPartyFactory.Create(party);
                rows += convertor.Convert(party);
            }
            return Content("Successfully recieved.", "application/json");
        }

        [HttpPost(nameof(PostCallHistoryEntry))]
        public IActionResult PostCallHistoryEntry([FromBody] List<CallHistoryEntryContract> callHistories)
        {
            int rows = 0;
            foreach (var callResult in callHistories)
            {
                rows += CallHistoryEntry.RecordHistory(callResult);
            }
            return Content("Not Implemented Yet", "application/json");
        }
    }
}
