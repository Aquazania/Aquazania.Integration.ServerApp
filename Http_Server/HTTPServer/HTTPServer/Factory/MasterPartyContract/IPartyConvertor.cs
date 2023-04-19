using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;

namespace HTTPServer.Factory.MasterPartyContract
{
    public interface IPartyConvertor
    {
        public Task Convert(ChangedPartyContactContract party);
        public bool ValidateParty(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction);
        public int UpdateRequired(ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction);
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, OdbcConnection connection, OdbcTransaction transaction);
    }
}
