using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.DeliveryAddress
{
    public class MasterDeliveryAddressParty
    {
        public async void SendMasterDeliveryAddressParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterDeliveryAddressObject(_DTS_connectionString);
            var url = "https://aquazania-telephony-in-func-demo.azurewebsites.net/api/AddParties";
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, url);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncDeliveryAddressMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncDeliveryAddressMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Temp Master Party Contract] "
                               + "	SET [Synced] = 1 "
                               + "WHERE PartyType = 'DeliveryAddress' AND "
                               + "	  PartyCode IN (SELECT PartyCode "
                               + "					FROM [Temp Master Party Contract] "
                               + "					WHERE [Synced] = 0 AND "
                               + "						  [PartyType] = 'DeliveryAddress' "
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
        private List<MasterOwnedPartyContract> buildMasterDeliveryAddressObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> DeliveryAddressUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'DeliveryAddress' "
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
                                    string sqlAcc = "SELECT * FROM [Delivery Address] WHERE [Delivery Address Code] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedPartyContract DeliveryAddress = new MasterOwnedPartyContract();
                                        DeliveryAddress.ParentPartyCode = null;
                                        DeliveryAddress.ParentPartyType = null;
                                        DeliveryAddress.ParentPartyFullName = null;
                                        DeliveryAddress.PartyCode = readerAcc["Account No"].ToString();
                                        DeliveryAddress.PartyType = "Customer";
                                        DeliveryAddress.PartyFullName = readerAcc["Account Name"].ToString();
                                        DeliveryAddress.PartyPrimaryContactFullName = readerAcc["Creditors Clerk"].ToString();
                                        DeliveryAddress.PartyPrimaryTelephoneNumber = readerAcc["Telephone No"].ToString();
                                        DeliveryAddress.PartyPrimaryCellNumber = readerAcc["Cell Phone No"].ToString();
                                        DeliveryAddress.IsActive = true;
                                        DeliveryAddressUpdates.Add(DeliveryAddress);
                                    }
                                }
                                catch (OdbcException ex)
                                {
                                    throw ex;
                                }
                            }
                        }
                        return DeliveryAddressUpdates;
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
