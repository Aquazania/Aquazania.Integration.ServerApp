using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.Net.Http;

namespace HTTPServer.Client.Customer
{
    public class MasterCustomerParty
    {
        public MasterCustomerParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterCustomerParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterCustomerObject(_DTS_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncCustomerMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncCustomerMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Temp Master Party Contract] "
                               + "	SET [Synced] = 1 "
                               + "WHERE PartyType = 'Customer' AND "
                               + "	  PartyCode IN (SELECT PartyCode "
                               + "					FROM [Temp Master Party Contract] "
                               + "					WHERE [Synced] = 0 AND "
                               + "						  [PartyType] = 'Customer' "
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
        private List<MasterOwnedPartyContract> buildMasterCustomerObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> customerUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'Customer' "
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
}
