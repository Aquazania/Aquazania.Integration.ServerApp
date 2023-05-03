using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract;

namespace Aquazania.Integration.ServerApp.Factory.MasterPartyContract
{
    public abstract class AbsParty : IPartyConvertor
    {
        public async Task<List<string>> Convert(ChangedPartyContactContract party, IConfiguration configuration)
        {
            string _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");

            List<string> errors = SanityCheck(party, _DTS_connectionString);
            if (errors.Count() == 0)
                if (ValidateParty(party, _DTS_connectionString))
                    _ = UpdateRequired(party, _DTS_connectionString) > 0;
                else
                {
                    errors.Add("Party Code Was Not Found In Database");
                }
            return errors;
        }
        public abstract int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, string _DTS_connectionString);
        public abstract List<string> SanityCheck(ChangedPartyContactContract party, string _DTS_connectionString);
        public abstract int UpdateRequired(ChangedPartyContactContract party, string _DTS_connectionString);
        public abstract bool ValidateParty(ChangedPartyContactContract party, string _DTS_connectionString);
    }
}
