using Aquazania.Telephony.Integration.Models;
using Microsoft.Extensions.Configuration;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class ContactParty : IPartyConvertor
    {
        private string _DTS_connectionString;
        private string _COM_connectionString;

        public ContactParty(IConfiguration configuration)
        {
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            _COM_connectionString = configuration.GetConnectionString("_COM_connectionString");
        }

        public int Convert(ChangedPartyContactContract party)
        {
            throw new NotImplementedException();
        }

        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string accountNo)
        {
            throw new NotImplementedException();
        }

        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party)
        {
            throw new NotImplementedException();
        }

        public int UpdateRequired(ChangedPartyContactContract party)
        {
            throw new NotImplementedException();
        }

        public bool ValidateParty(ChangedPartyContactContract party)
        {
            throw new NotImplementedException();
        }
    }
}
