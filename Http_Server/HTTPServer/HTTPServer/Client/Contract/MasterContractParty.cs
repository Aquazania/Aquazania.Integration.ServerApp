using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.Contract
{
    public class MasterContractParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
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
        public override List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> contractUpdates = new List<MasterOwnedPartyContract>();
            try
            {
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
                        using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
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
                                        contract.AccountCode = readerAcc["Account No"].ToString();
                                        contract.AccountName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    {
                                        contract.ParentPartyCode = null;
                                        contract.ParentPartyType = null;
                                        contract.ParentPartyFullName = null;
                                        contract.AccountCode = null;
                                        contract.AccountName = null;
                                    }
                                    contract.PartyCode = readerAcc["Contract No"].ToString();
                                    contract.PartyType = "Contract";
                                    contract.PartyFullName = readerAcc["Account Name"].ToString();
                                    contract.PartyPrimaryContactFullName = readerAcc["Creditors Clerk"].ToString();
                                    contract.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Telephone No"].ToString(), @"\D", "");
                                    contract.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell Phone No"].ToString(), @"\D", "");
                                    contract.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterParty.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(contract + ",", Formatting.Indented));
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
