using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Contact
{
    public class MasterContactParty
    {
        public MasterContactParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterContactParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterContactObject(_DTS_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncContactMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncContactMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Temp Master Party Contract] "
                               + "	SET [Synced] = 1 "
                               + "WHERE PartyType = 'Contact' AND "
                               + "	  PartyCode IN (SELECT PartyCode "
                               + "					FROM [Temp Master Party Contract] "
                               + "					WHERE [Synced] = 0 AND "
                               + "						  [PartyType] = 'Contact' "
                               + "					GROUP BY PartyCode) ";
                    var command = new OdbcCommand(sql, connection);
                    int rows = command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        private List<MasterOwnedPartyContract> buildMasterContactObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> contactUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'Contact' "
                               + "GROUP BY PartyCode ";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
                            {
                                try
                                {
                                    connectionAcc.Open();
                                    string sqlAcc = "SELECT * FROM [Contact] WHERE [Contact No] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedPartyContract contact = new MasterOwnedPartyContract();
                                        contact.ParentPartyCode = null;
                                        contact.ParentPartyType = null;
                                        contact.ParentPartyFullName = null;
                                        contact.PartyCode = readerAcc["Contact No"].ToString();
                                        contact.PartyType = "Contact";
                                        contact.PartyFullName = readerAcc["Company"].ToString();
                                        contact.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();
                                        contact.PartyPrimaryTelephoneNumber = readerAcc["Telephone No"].ToString();
                                        contact.PartyPrimaryCellNumber = readerAcc["Cell Phone No"].ToString();
                                        contact.IsActive = true;
                                        contactUpdates.Add(contact);
                                    }
                                }
                                catch (OdbcException ex)
                                {
                                    throw ex;
                                }
                            }
                        }
                        return contactUpdates;
                    }
                    else
                    {
                        return new List<MasterOwnedPartyContract>();
                    }
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
