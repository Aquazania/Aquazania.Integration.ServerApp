﻿using Aquazania.Telephony.Integration.Models;
using HTTPServer.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using HTTPServer.Factory.MasterPartyContract;
using HTTPServer.Factory.MasterLinkedPartyContract;

namespace HTTPServer.Controllers
{
    public class PartyController : ApiControllerBase
    {
        [HttpPost(nameof(PostParty))]
        public async Task<IActionResult> PostParty([FromBody] List<ChangedPartyContactContract> parties)
        {
            foreach (var party in parties)
            {
                var convertor = PartyFactory.Create(party);
                await convertor.Convert(party);
            }
            return Content("Successfully recieved.", "application/json");
        }

        [HttpPost(nameof(PostLinkedParty))]
        public async Task<IActionResult> PostLinkedParty([FromBody] List<ChangedLinkedContactContract> parties)
        {
            foreach (var party in parties)
            {
                var convertor = LinkedPartyFactory.Create(party);
                await convertor.Convert(party);
            }
            return Content("Successfully recieved.", "application/json");
        }
    }
}
