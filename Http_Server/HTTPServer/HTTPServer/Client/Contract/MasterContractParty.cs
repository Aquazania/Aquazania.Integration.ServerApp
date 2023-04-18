using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Contract
{
    public class MasterContractParty : IMasterParty
    {
        public MasterContractParty(string url) { darielURL = url; }
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
                            + "WHERE PartyType = 'Contract' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Contract' "
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
            List<MasterOwnedPartyContract> contractUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                connection.Open();
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Contract' "
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
                                                "FROM [Contract] T1 " +
                                                "   LEFT JOIN [Customer] T2 ON " +
                                                "       T1.[Account No] = T2.[Account No] " +
                                                "WHERE [Contract No] = '" + reader["PartyCode"].ToString() + "'";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract contract = new MasterOwnedPartyContract();
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        contract.ParentPartyCode = readerAcc["Account No"].ToString();
                                        contract.ParentPartyType = "Customer";
                                        contract.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    {
                                        contract.ParentPartyCode = null;
                                        contract.ParentPartyType = null;
                                        contract.ParentPartyFullName = null;
                                    }
                                    contract.PartyCode = readerAcc["Contract No"].ToString();
                                    contract.PartyType = "Contract";
                                    contract.PartyFullName = readerAcc["Account Name"].ToString();
                                    contract.PartyPrimaryContactFullName = readerAcc["Creditors Clerk"].ToString();
                                    contract.PartyPrimaryTelephoneNumber = readerAcc["Telephone No"].ToString();
                                    contract.PartyPrimaryCellNumber = readerAcc["Cell Phone No"].ToString();
                                    contract.IsActive = true;
                                    contractUpdates.Add(contract);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return contractUpdates;
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
