using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Supplier
{
    public class MasterSupplierParty : IMasterParty
    {
        public MasterSupplierParty(string url) { darielURL = url; }
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
                            + "WHERE PartyType = 'Supplier' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Supplier' "
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
            List<MasterOwnedPartyContract> SupplierUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                connection.Open();
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Supplier' "
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
                                                "       T2.[Account Name]" +
                                                "FROM [Supplier] T1 " +
                                                "   LEFT JOIN [Customer] T2 ON " +
                                                "       T1.[Account No] = T2.[Account No] " +
                                                "WHERE [Supplier No] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract supplier = new MasterOwnedPartyContract();
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        supplier.ParentPartyCode = readerAcc["Account No"].ToString();
                                        supplier.ParentPartyType = "Customer";
                                        supplier.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    { 
                                        supplier.ParentPartyCode = null;
                                        supplier.ParentPartyType = null;
                                        supplier.ParentPartyFullName = null;
                                    }
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
