﻿using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client.Contract
{
    public class MasterContractLinkedParty
    {
        public async void SendMasterContractLinkedParty(ITimed_Client _httpClient, string _COM_connectionString)
        {
            var url = "https://aquazania-telephony-in-func-demo.azurewebsites.net/api/AddParties";
            var data = buildMasterContractLinkObject(_COM_connectionString);
            if (data.Count > 0)
            {
                var response = await _httpClient.SendAsync(data, url);

                if (response.IsSuccessStatusCode)
                {
                    UpdateSyncContractLinkMasterTable(_COM_connectionString);
                }
            }
        }

        private void UpdateSyncContractLinkMasterTable(string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
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
        private List<MasterOwnedLinkedContactContract> buildMasterContractLinkObject(string _COM_connectionString)
        {
            List<MasterOwnedLinkedContactContract> contractUpdates = new List<MasterOwnedLinkedContactContract>();
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT PartyCode "
                               + "FROM [Temp Master Party Contract] "
                               + "WHERE [Synced] = 0 AND "
                               + "	    [PartyType] = 'Contract' "
                               + "GROUP BY PartyCode ";
                    var command = new OdbcCommand(sql, connection);
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
                                    string sqlAcc = "SELECT * FROM [viewContactDocumentReference] WHERE [DocumentReferenceCode] = '" + reader["PartyCode"].ToString() + "'";
                                    var commandAcc = new OdbcCommand(sqlAcc, connectionAcc);
                                    var readerAcc = commandAcc.ExecuteReader();
                                    while (readerAcc.Read())
                                    {
                                        MasterOwnedLinkedContactContract contract = new MasterOwnedLinkedContactContract();
                                        contract.ParentPartyCode = readerAcc["DocumentReferenceCode"].ToString();
                                        contract.ParentPartyType = "Contract";
                                        contract.ContactFullName = readerAcc["ContactName"].ToString() + " " + (!readerAcc.IsDBNull(readerAcc.GetOrdinal("ContactLastName")) ? readerAcc["ContactLastName"].ToString() : "");
                                        contract.PhoneNumber = readerAcc["ContactPointValue"].ToString();
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
}