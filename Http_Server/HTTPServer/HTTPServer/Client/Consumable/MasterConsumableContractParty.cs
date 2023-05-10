using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.Consumable
{
    public class MasterConsumableContractParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'ContractConsumable' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'ContractConsumable' "
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
            List<MasterOwnedPartyContract> ConsumablesUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'ContractConsumable' "
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
                                string sqlAcc = "SELECT *  " +
                                                "FROM [Contract]  " +
                                                "WHERE [Contract No] = " + reader["PartyCode"].ToString();
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract Consumable = new MasterOwnedPartyContract();
                                    Consumable.ParentPartyCode = readerAcc["Contract No"].ToString();
                                    Consumable.ParentPartyType = "Contract";
                                    Consumable.ParentPartyFullName = readerAcc["Account Name"].ToString();
                                    int accountNoIndex = readerAcc.GetOrdinal("Account No");
                                    if (!readerAcc.IsDBNull(accountNoIndex))
                                    {
                                        Consumable.AccountCode = readerAcc["Account No"].ToString();
                                        Consumable.AccountName = readerAcc["Account Name"].ToString();
                                    }
                                    Consumable.PartyCode = null;
                                    Consumable.PartyType = "Consumable";
                                    Consumable.PartyFullName = readerAcc["Account Name"].ToString();
                                    Consumable.PartyPrimaryContactFullName = readerAcc["Consumables Contact Person"].ToString();
                                    Consumable.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Tel No For Consumables Contact Person"].ToString(), @"\D", "");
                                    Consumable.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell No For Consumables Contact Person"].ToString(), @"\D", "");
                                    Consumable.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterPartyContractConsumable.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(Consumable, Formatting.Indented) + ",");
                                    ConsumablesUpdates.Add(Consumable);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return ConsumablesUpdates;
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
