using Aquazania.Telephony.Integration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Handlers.Handlers.impl
{
    internal class PartyHandler : IPartyHandler
    {

        private readonly IPartyHandler _partyHandler;
        public PartyHandler(IPartyHandler ) { }
        public void PublishPartyUpdate(PartyContract party)
        {
            throw new NotImplementedException();
        }
    }
}
