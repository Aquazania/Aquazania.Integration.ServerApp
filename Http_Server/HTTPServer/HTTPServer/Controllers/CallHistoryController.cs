using Aquazania.Integration.ServerApp.PostCallHistoryEntryContract;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Aquazania.Integration.ServerApp.Controllers
{
    public class CallHistoryController : ApiControllerBase
    {
        [HttpPost(nameof(PostCallHistoryEntry))]
        public async Task<IActionResult> PostCallHistoryEntry([FromBody] List<CallHistoryEntryContract> callHistories)
        {
            int rows = 0;
            foreach (var callResult in callHistories)
            {
                CallHistoryEntry callHistory = new CallHistoryEntry();
                callHistory.RecordHistory(callResult);
            }
            return Content("Not Implemented Yet", "application/json");
        }
    }
}
