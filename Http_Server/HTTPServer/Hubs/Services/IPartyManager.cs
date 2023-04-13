using Aquazania.Telephony.Integration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hubs.Services
{
    public interface IPartyManager
    {
        void PublishPartyUpdate(PartyContract party);
    }
}
