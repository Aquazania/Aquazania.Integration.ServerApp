using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Contact
{
    public class MasterContactLinkedParty : IMasterLinkedParty
    {
        public MasterContactLinkedParty(string url) { darielURL = url; }   
        private string darielURL;
        public async void SendMasterLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var data = buildMasterLinkObject(connection, transaction);
                        if (data.Count > 0)
                        {
                            var response = await _httpClient.SendAsync(data, darielURL);

                            if (response.IsSuccessStatusCode)
                            {
                                UpdateSyncLinkMasterTable(connection, transaction);
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction)
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
        public List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction)
        {
            List<MasterOwnedLinkedContactContract> contactUpdates = new List<MasterOwnedLinkedContactContract>();
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
                        using (var connectionAcc = new OdbcConnection(connection.ConnectionString))
                        {
                            try
                            {
                                connectionAcc.Open();
                                string sqlAcc = "SELECT * " +
                                                "FROM [viewContactDocumentReference] " +
                                                "WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "' " +
                                                "  AND [ContactPointTypeID] = 2 ";
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
