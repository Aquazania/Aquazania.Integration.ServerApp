using Aquazania.Integration.ServerApp.Factory;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client
{
    public interface IMasterLinkedParty
    {
        public Task SendMasterLinkedParty(ITimed_Client _httpClient, string _COM_connectionString, string _DTS_connectionString);
        public void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction);
        public List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString);
        public void LogUnsuccessfulRequest(List<MasterOwnedLinkedContactContract> payload, HttpResponseMessage response, string failedContracts, string _COM_connectionString, DarielResponse message);
    }
}
