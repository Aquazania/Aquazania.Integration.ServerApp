using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Contract
{
    public class MasterContractParty
    {
        public MasterContractParty(string url) { darielURL = url; }
        private string darielURL;
        public async void SendMasterContractParty(ITimed_Client _httpClient, string _DTS_connectionString)
        {
            var data = buildMasterContractObject(_DTS_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, darielURL);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncContractMasterTable(_DTS_connectionString);
                }
            }
        }
        private void UpdateSyncContractMasterTable(string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
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
                    int rows = command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        private List<MasterOwnedPartyContract> buildMasterContractObject(string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> contractUpdates = new List<MasterOwnedPartyContract>();
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	  [PartyType] = 'Contract' "
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
                                    string sqlAcc = "SELECT * FROM [Contract] WHERE [Contract No] = '" + reader["PartyCode"].ToString() + "'";
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
                                        }
                                        else
                                        {
                                            contract.ParentPartyCode = null;
                                            contract.ParentPartyType = null;
                                        }
                                        contract.ParentPartyFullName = null;
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
}
