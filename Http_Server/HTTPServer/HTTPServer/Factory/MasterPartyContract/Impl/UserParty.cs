using Aquazania.Telephony.Integration.Models;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class UserParty : IPartyConvertor
    {
        private string _DTS_connectionString;
        private string _COM_connectionString;

        public UserParty(IConfiguration configuration)
        {
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            _COM_connectionString = configuration.GetConnectionString("COM_connectionString");
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
