using Aquazania.Integration.ServerApp.Client;
using Aquazania.Telephony.Integration.Models;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Diagnostics.Contracts;
using System.Net.Http;
using Aquazania.Integration.ServerApp.Factory;
using System.Text.RegularExpressions;
using System;

namespace HTTPServer.Client.Customer
{
    public class MasterCustomerLinkedParty : AbsMasterLinkedParty
    {
        public override void UpdateSyncLinkMasterTable(OdbcConnection connection, OdbcTransaction transaction)
        {
            try
            {
                string sql = "UPDATE [Temp Master Party Contract] "
                            + "	SET [Synced] = 1 "
                            + "WHERE PartyType = 'Customer' AND "
                            + "	  PartyCode IN (SELECT PartyCode "
                            + "					FROM [Temp Master Party Contract] "
                            + "					WHERE [Synced] = 0 AND "
                            + "						  [PartyType] = 'Customer' "
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
        public override List<MasterOwnedLinkedContactContract> buildMasterLinkObject(OdbcConnection connection, OdbcTransaction transaction, string _COM_connectionString, string _DTS_connectionString)
        {
            List<MasterOwnedLinkedContactContract> customerUpdates = new List<MasterOwnedLinkedContactContract>();
            try
            {
                string sql = "SELECT PartyCode "
                            + "FROM [Temp Master Party Contract] "
                            + "WHERE [Synced] = 0 AND "
                            + "	    [PartyType] = 'Customer' "
                            + "GROUP BY PartyCode ";
                var command = new OdbcCommand(sql, connection);
                command.Transaction = transaction;
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
                                string sqlAcc = "SELECT * " +
                                                "FROM [viewContactDocumentReference] " +
                                                "WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "'" +
                                                "  AND [ContactPointTypeID] = 2";
                                var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                var readerAcc = commandAcc.ExecuteReader();
                                string prevAccountNo = null;
                                string accName = null;
                                string accNo = null;
                                while (readerAcc.Read())
                                {
                                    MasterOwnedLinkedContactContract customer = new MasterOwnedLinkedContactContract();
                                    string curAccountNo = readerAcc["DocumentReferenceCode"].ToString();
                                    if (prevAccountNo != curAccountNo)
                                    {
                                        using (var connectionAccountInfo = new OdbcConnection(_DTS_connectionString))
                                        {
                                            try
                                            {
                                                string sqlAccInfo = "SELECT * FROM [Customer] WHERE [Account No] = '" + readerAcc["DocumentReferenceCode"].ToString() + "'";
                                                connectionAccountInfo.Open();
                                                var commandAccInfo = new OdbcCommand(sqlAccInfo, connectionAccountInfo);
                                                var readerAccInfo = commandAccInfo.ExecuteReader();
                                                if (readerAccInfo.HasRows)
                                                {
                                                    while (readerAccInfo.Read())
                                                    {
                                                        int accountNoIndex = readerAccInfo.GetOrdinal("Account No");
                                                        if (!readerAccInfo.IsDBNull(accountNoIndex))
                                                        {
                                                            customer.AccountCode = readerAccInfo["Account No"].ToString();
                                                            customer.AccountName = readerAccInfo["Account Name"].ToString();
                                                            accNo = readerAccInfo["Account No"].ToString();
                                                            accName = readerAccInfo["Account Name"].ToString();
                                                        }
                                                    }
                                                }
                                                else
                                                { customer.AccountName = null; customer.AccountCode = null; }
                                            }
                                            catch (OdbcException ex) { throw ex; }
                                        }
                                    }
                                    else
                                    { customer.AccountCode = accNo; customer.AccountName = accName; }
                                    customer.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                    customer.ParentPartyType = "Customer";
                                    customer.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                    customer.PhoneNumber = Regex.Replace(readerAcc["ContactPointValue"].ToString(), @"\D", "");
                                    customer.IsActive = true;
                                    customerUpdates.Add(customer);
                                    string filePath = @"C:\Tracking Folder\MasterLinkedPartyCustomer.txt";
                                    using (StreamWriter writer = new StreamWriter(filePath, true))
                                    {
                                        writer.WriteLine();
                                    }
                                    File.AppendAllText(filePath, JsonConvert.SerializeObject(customer, Formatting.Indented) + ",");
                                    prevAccountNo = curAccountNo;
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
