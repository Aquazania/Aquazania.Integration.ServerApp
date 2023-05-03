using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;

namespace HTTPServer.Factory.MasterLinkedPartyContract
{
    public interface ILinkedPartyConvertor
    {
        public Task<List<string>> Convert(ChangedLinkedContactContract party, IConfiguration configuration);
        public int ValidateParty(ChangedLinkedContactContract party, string _COM_connectionString, string _DTS_connectionString);
        public int UpdateRequired(ChangedLinkedContactContract party, string _COM_connectionString, int updatetype = 1);
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID, string _COM_connectionString);
        public int DoInsert(ChangedLinkedContactContract party, string _COM_connectionString, int updatetype = 1);
        public List<string> SanityCheck(ChangedLinkedContactContract party, string _DTS_connectionString);
    }
}
