using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.User
{
    public class MasterUserParty
    {
        public async void SendMasterUserParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterUserObject(_DTS_connectionString);
            var url = "https://aquazania-telephony-in-func-demo.azurewebsites.net/api/AddParties";
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, url);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncUserMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncUserMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
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
        private List<MasterOwnedPartyContract> buildMasterUserObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> userUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'User' "
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
                                    string sqlAcc = "SELECT * FROM [User] WHERE [User Name] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedPartyContract user = new MasterOwnedPartyContract();
                                        if (!readerAcc.IsDBNull(reader.GetOrdinal("Account No")))
                                        {
                                            user.ParentPartyCode = readerAcc["Account No"].ToString();
                                            user.ParentPartyType = "Customer";
                                        }
                                        else
                                        {
                                            user.ParentPartyCode = null;
                                            user.ParentPartyType = null;
                                        }
                                        user.ParentPartyFullName = null;
                                        user.PartyCode = readerAcc["User Name"].ToString();
                                        user.PartyType = "User";
                                        user.PartyFullName = readerAcc["First Name"].ToString() + " " + readerAcc["Last Name"].ToString();
                                        user.PartyPrimaryContactFullName = readerAcc["First Name"].ToString() + " " + readerAcc["Last Name"].ToString();
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
}
