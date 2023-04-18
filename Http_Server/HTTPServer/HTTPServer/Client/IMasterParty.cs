using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client
{
    public interface IMasterParty
    {
        public Task SendMasterParty(ITimed_Client _httpClient, string _DTS_connectionString);
        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction);
        public List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString);
        public void LogUnsuccessfulRequest(string _DTS_connectionString, List<MasterOwnedPartyContract> payload, HttpResponseMessage response, string failedContracts);
    }
}
