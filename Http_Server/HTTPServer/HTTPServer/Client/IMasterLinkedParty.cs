using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client
{
    public interface IMasterLinkedParty
    {
        public void SendMasterLinkedParty(ITimed_Client _httpClient, string _COM_connectionString);
        public void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction);
        public List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction);
        public void LogUnsuccessfulRequest(string _COM_connectionString, List<MasterOwnedLinkedContactContract> payload, HttpResponseMessage response);
    }
}
