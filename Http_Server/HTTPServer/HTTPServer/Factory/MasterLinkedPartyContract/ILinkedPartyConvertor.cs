using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;

namespace HTTPServer.Factory.MasterLinkedPartyContract
{
    public interface ILinkedPartyConvertor
    {
        public Task<List<string>> Convert(ChangedLinkedContactContract party);
        public int ValidateParty(ChangedLinkedContactContract party);
        public int UpdateRequired(ChangedLinkedContactContract party, int updatetype = 1);
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID);
        public int DoInsert(ChangedLinkedContactContract party, int updatetype = 1);
        public List<string> SanityCheck(ChangedLinkedContactContract party);
    }
}
