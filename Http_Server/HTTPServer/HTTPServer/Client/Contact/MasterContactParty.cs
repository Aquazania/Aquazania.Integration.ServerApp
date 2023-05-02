using Aquazania.Integration.ServerApp.Factory;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using Newtonsoft.Json;
using System.Data;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace Aquazania.Integration.ServerApp.Client.Contact
{
    public class MasterContactParty : AbsMasterParty
    {
        public override void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'Contact' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Contact' "
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
            List<MasterOwnedPartyContract> contactUpdates = new List<MasterOwnedPartyContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	  [PartyType] = 'Contact' "
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
                                string sqlAcc = "SELECT * " +
                                                "FROM [Contact] " +
                                                "WHERE [Contact No] = " + reader["PartyCode"].ToString();
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                while (readerAcc.Read())
                                {
                                    MasterOwnedPartyContract contact = new MasterOwnedPartyContract();
                                    contact.ParentPartyCode = null;
                                    contact.ParentPartyType = null;
                                    contact.ParentPartyFullName = null;
                                    contact.PartyCode = readerAcc["Contact No"].ToString();
                                    contact.PartyType = "Contact";
                                    contact.AccountCode = null;
                                    contact.AccountName = null;
                                    contact.PartyFullName = readerAcc["Company"].ToString();
                                    contact.PartyPrimaryContactFullName = readerAcc["Contact Person"].ToString();
                                    contact.PartyPrimaryTelephoneNumber = Regex.Replace(readerAcc["Telephone No"].ToString(), @"\D", "");
                                    contact.PartyPrimaryCellNumber = Regex.Replace(readerAcc["Cell Phone No"].ToString(), @"\D", "");
                                    contact.IsActive = true;
                                    string filePath = @"C:\Tracking Folder\MasterParty.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(contact + ",", Formatting.Indented));
                                    contactUpdates.Add(contact);
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    return contactUpdates;
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
