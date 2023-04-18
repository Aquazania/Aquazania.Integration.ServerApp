using Aquazania.Integration.ServerApp.Client;
using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.Net.Http;

namespace HTTPServer.Client.Customer
{
    public class MasterCustomerParty : IMasterParty
    {
        public MasterCustomerParty(string url) { darielURL = url; }
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
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'Customer' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Customer' "
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
            List<MasterOwnedPartyContract> customerUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Customer' "
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
                                string sqlAcc = "SELECT * FROM [Customer] WHERE [Account No] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract customer = new MasterOwnedPartyContract();
                                    customer.ParentPartyCode = null;
                                    customer.ParentPartyType = null;
                                    customer.ParentPartyFullName = null;
                                    customer.PartyCode = readerAcc["Account No"].ToString();
                                    customer.PartyType = "Customer";
                                    customer.PartyFullName = readerAcc["Account Name"].ToString();
                                    customer.PartyPrimaryContactFullName = readerAcc["Creditors Clerk"].ToString();
                                    customer.PartyPrimaryTelephoneNumber = readerAcc["Telephone No"].ToString();
                                    customer.PartyPrimaryCellNumber = readerAcc["Cell Phone No"].ToString();
                                    customer.IsActive = true;
                                    customerUpdates.Add(customer);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return customerUpdates;
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
