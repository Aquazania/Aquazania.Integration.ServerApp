using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;

namespace HTTPServer.Factory.MasterLinkedPartyContract
{
    public interface ILinkedPartyConvertor
    {
        public Task Convert(ChangedLinkedContactContract party);
        public int ValidateParty(ChangedLinkedContactContract party, OdbcConnection connection, OdbcTransaction transaction);
        public int UpdateRequired(ChangedLinkedContactContract party, OdbcConnection connection, OdbcTransaction transaction, int updatetype = 1);
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID, OdbcConnection connection, OdbcTransaction transaction);
        public int DoInsert(ChangedLinkedContactContract party, OdbcConnection connection, OdbcTransaction transaction, int updatetype = 1);
    }
}
