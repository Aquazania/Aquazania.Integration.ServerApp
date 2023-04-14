using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Supplier
{
    public class MasterSupplierParty
    {
        public MasterSupplierParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterSupplierParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterSupplierObject(_DTS_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncSupplierMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncSupplierMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Temp Master Party Contract] "
                               + "	SET [Synced] = 1 "
                               + "WHERE PartyType = 'Supplier' AND "
                               + "	  PartyCode IN (SELECT PartyCode "
                               + "					FROM [Temp Master Party Contract] "
                               + "					WHERE [Synced] = 0 AND "
                               + "						  [PartyType] = 'Supplier' "
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
        private List<MasterOwnedPartyContract> buildMasterSupplierObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> SupplierUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'Supplier' "
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
                                    string sqlAcc = "SELECT * FROM [Supplier] WHERE [Supplier No] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedPartyContract supplier = new MasterOwnedPartyContract();
                                        if (!readerAcc.IsDBNull(reader.GetOrdinal("Account No")))
                                        {
                                            supplier.ParentPartyCode = readerAcc["Account No"].ToString();
                                            supplier.ParentPartyType = "Customer";
                                        }
                                        else
                                        { 
                                            supplier.ParentPartyCode = null;
                                            supplier.ParentPartyType = null;
                                        }
                                        supplier.ParentPartyFullName = null;
                                        supplier.PartyCode = readerAcc["Supplier No"].ToString();
                                        supplier.PartyType = "Supplier";
                                        supplier.PartyFullName = readerAcc["Supplier Name"].ToString();
                                        supplier.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();
                                        supplier.PartyPrimaryTelephoneNumber = readerAcc["Telephone No"].ToString();
                                        supplier.PartyPrimaryCellNumber = readerAcc["Cell Phone No"].ToString();
                                        supplier.IsActive = true;
                                        SupplierUpdates.Add(supplier);
                                    }
                                }
                                catch (OdbcException ex)
                                {
                                    throw ex;
                                }
                            }
                        }
                        return SupplierUpdates;
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
