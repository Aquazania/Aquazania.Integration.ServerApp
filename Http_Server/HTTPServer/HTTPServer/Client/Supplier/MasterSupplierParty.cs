using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.Supplier
{
    public class MasterSupplierParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
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
        public override List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction, string _DTS_connectionString)
        {
            List<MasterOwnedPartyContract> SupplierUpdates = new List<MasterOwnedPartyContract>();
            try
            {
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
                        using (var connectionAcc = new OdbcConnection(_DTS_connectionString))
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
                                        supplier.AccountCode = readerAcc["Account No"].ToString();
                                        supplier.AccountName = readerAcc["Account Name"].ToString();
                                    }
                                    else
                                    { 
                                        supplier.ParentPartyCode = null;
                                        supplier.ParentPartyType = null;
                                        supplier.ParentPartyFullName = null;
                                        supplier.AccountCode = null;
                                        supplier.AccountName = null;
                                    }
                                    supplier.PartyCode = readerAcc["Supplier No"].ToString();
                                    supplier.PartyType = "Supplier";
                                    supplier.PartyFullName = readerAcc["Supplier Name"].ToString();                                    
                                    supplier.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();                                    
                                    supplier.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Telephone No"].ToString(), @"\D", "");                                    
                                    supplier.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell Phone No"].ToString(), @"\D", "");
                                    supplier.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterParty.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(supplier + ",", Formatting.Indented));
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
