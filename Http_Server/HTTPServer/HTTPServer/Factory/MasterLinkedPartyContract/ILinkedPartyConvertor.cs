using Aquazania.Telephony.Integration.Models;

namespace HTTPServer.Factory.MasterLinkedPartyContract
{
    public interface ILinkedPartyConvertor
    {
        public int Convert(ChangedLinkedContactContract party);
        public bool ValidateParty(ChangedLinkedContactContract party);
        public int UpdateRequired(ChangedLinkedContactContract party);
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID);
        public int DoInsert(ChangedLinkedContactContract party);
    }
}
