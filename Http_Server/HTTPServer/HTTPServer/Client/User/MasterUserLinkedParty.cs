using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.User
{
    public class MasterUserLinkedParty
    {
        public MasterUserLinkedParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterUserLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            var data = buildMasterUserLinkObject(_COM_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncUserLinkMasterTable(_COM_connectionString);
                }
            }
        }

        private void UpdateSyncUserLinkMasterTable(string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Temp Master Party Contract] "
                               + "	SET [Synced] = 1 "
                               + "WHERE PartyType = 'User' AND "
                               + "	  PartyCode IN (SELECT PartyCode "
                               + "					FROM [Temp Master Party Contract] "
                               + "					WHERE [Synced] = 0 AND "
                               + "						  [PartyType] = 'User' "
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
        private List<MasterOwnedLinkedContactContract> buildMasterUserLinkObject(string _COM_connectionString)
        {
            List<MasterOwnedLinkedContactContract> userUpdates = new List<MasterOwnedLinkedContactContract>();
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	    [PartyType] = 'User' "
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
                                        MasterOwnedLinkedContactContract user = new MasterOwnedLinkedContactContract();
                                        user.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                        user.ParentPartyType = "User";
                                        user.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                        user.PhoneNumber = readerAcc["ContactPointValue"].ToString();
                                        user.IsActive = true;
                                        userUpdates.Add(user);
                                    }
                                }
                                catch (OdbcException ex)
                                {
                                    throw ex;
                                }
                            }
                        }
                        return userUpdates;
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
