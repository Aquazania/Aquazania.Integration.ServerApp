using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;

namespace HTTPServer.Factory.MasterPartyContract
{
    public interface IPartyConvertor
    {
        public Task<List<string>> Convert(ChangedPartyContactContract party, IConfiguration configuration);
        public bool ValidateParty(ChangedPartyContactContract party, string _DTS_connectionString);
        public int UpdateRequired(ChangedPartyContactContract party, string _DTS_connectionString);
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, string _DTS_connectionString);
        public List<string> SanityCheck(ChangedPartyContactContract party, string _DTS_connectionString);
    }
}
