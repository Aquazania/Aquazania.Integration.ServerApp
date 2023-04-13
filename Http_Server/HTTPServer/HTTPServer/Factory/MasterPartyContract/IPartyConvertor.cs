using Aquazania.Telephony.Integration.Models;

namespace HTTPServer.Factory.MasterPartyContract
{
    public interface IPartyConvertor
    {
        public int Convert(ChangedPartyContactContract party);
        public bool ValidateParty(ChangedPartyContactContract party);
        public int UpdateRequired(ChangedPartyContactContract party);
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party);
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string accountNo);
    }
}
