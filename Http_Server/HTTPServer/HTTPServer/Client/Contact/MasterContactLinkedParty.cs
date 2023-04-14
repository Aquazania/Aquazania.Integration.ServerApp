using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Contact
{
    public class MasterContactLinkedParty
    {
        public MasterContactLinkedParty(string url) { darielURL = url; }   
        private string darielURL;
        public async void SendMasterContactLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            var data = buildMasterContactLinkObject(_COM_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncContactLinkMasterTable(_COM_connectionString);
                }
            }
        }

        private void UpdateSyncContactLinkMasterTable(string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
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
        private List<MasterOwnedLinkedContactContract> buildMasterContactLinkObject(string _COM_connectionString)
        {
            List<MasterOwnedLinkedContactContract> contactUpdates = new List<MasterOwnedLinkedContactContract>();
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	    [PartyType] = 'Contact' "
                               + "GROUP BY PartyCode ";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            using (var connectionAcc = new OdbcConnection(_COM_connectionString))
                            {
                                try
                                {
                                    connectionAcc.Open();
                                    string sqlAcc = "SELECT * FROM [viewContactDocumentReference] WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedLinkedContactContract contact = new MasterOwnedLinkedContactContract();
                                        contact.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                        contact.ParentPartyType = "Contact";
                                        contact.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                        contact.PhoneNumber = readerAcc["ContactPointValue"].ToString();
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
                        return new List<MasterOwnedLinkedContactContract>();
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
