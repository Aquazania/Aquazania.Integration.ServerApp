using Aquazania.Telephony.Integration.Models;

namespace HTTPServer.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedContractParty : ILinkedPartyConvertor
    {
        public int Convert(ChangedLinkedContactContract party)
        {
            throw new NotImplementedException();
        }

        public int DoInsert(ChangedLinkedContactContract party)
        {   
            throw new NotImplementedException();
        }

        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID)
        {
            throw new NotImplementedException();
        }

        public int UpdateRequired(ChangedLinkedContactContract party)
        {
            throw new NotImplementedException();
        }

        public bool ValidateParty(ChangedLinkedContactContract party)
        {
            throw new NotImplementedException();
        }
    }
}
