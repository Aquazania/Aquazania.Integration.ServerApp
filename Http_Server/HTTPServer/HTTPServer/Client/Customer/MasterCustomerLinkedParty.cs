using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.Net.Http;

namespace HTTPServer.Client.Customer
{
    public class MasterCustomerLinkedParty
    {
        public async void SendMasterCustomerLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            var url = "https://aquazania-telephony-in-func-demo.azurewebsites.net/api/AddParties";
            var data = buildMasterCustomerLinkObject(_COM_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, url);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncCustomerLinkMasterTable(_COM_connectionString);
                }
            }
        }

        private void UpdateSyncCustomerLinkMasterTable(string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
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
        private List<MasterOwnedLinkedContactContract> buildMasterCustomerLinkObject(string _COM_connectionString)
        {
            List<MasterOwnedLinkedContactContract> customerUpdates = new List<MasterOwnedLinkedContactContract>();
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	    [PartyType] = 'Customer' "
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
                                        MasterOwnedLinkedContactContract customer = new MasterOwnedLinkedContactContract();
                                        customer.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                        customer.ParentPartyType = "Customer";
                                        customer.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                        customer.PhoneNumber = readerAcc["ContactPointValue"].ToString();
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
