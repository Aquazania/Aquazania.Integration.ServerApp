using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.User
{
    public class MasterUserParty : IMasterParty
    {
        public MasterUserParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var data = buildMasterObject(connection, transaction);
                        if (data.Count > 0)
                        {
                            var response = await _httpClient.SendAsync(data, darielURL);

                            if (response.IsSuccessStatusCode)
                            {
                                UpdateSyncMasterTable(connection, transaction);
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
        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
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
                command.Transaction = transaction;
                int rows = command.ExecuteNonQuery();
            }
            catch (OdbcException ex)
            {
                throw ex;
            }
        }
        public List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction)
        {
            List<MasterOwnedPartyContract> userUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                connection.Open();
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'User' "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
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
                                string sqlAcc = "SELECT T1.*, " +
                                                "       T2.[Account Name] " +
                                                "FROM [User] T1 " +
                                                "   LEFT JOIN [Customer] T2 ON " +
                                                "       T1.[Account No] = T2.[Account No] " +
                                                "WHERE [User Name] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract user = new MasterOwnedPartyContract();
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        user.ParentPartyCode = readerAcc["Account No"].ToString();
                                        user.ParentPartyType = "Customer";
                                        user.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    {
                                        user.ParentPartyCode = null;
                                        user.ParentPartyType = null;
                                        user.ParentPartyFullName = null;
                                    }
                                    user.PartyCode = readerAcc["User Name"].ToString();
                                    user.PartyType = "User";
                                    user.PartyFullName = readerAcc["First Name"].ToString() + " " + readerAcc["Surname"].ToString();
                                    user.PartyPrimaryContactFullName = readerAcc["First Name"].ToString() + " " + readerAcc["Surname"].ToString();
                                    user.PartyPrimaryTelephoneNumber = readerAcc["Telephone No"].ToString();
                                    user.PartyPrimaryCellNumber = readerAcc["Cell Phone No"].ToString();
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
