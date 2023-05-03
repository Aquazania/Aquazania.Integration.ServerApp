using Aquazania.Integration.ServerApp.Factory.MasterPartyContract.Impl;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract.Impl;
using System.Configuration;
using System.Data.Odbc;
using System.IO;

namespace Aquazania.Integration.ServerApp.PostCallHistoryEntryContract
{
    public class CallHistoryEntry
    {
        enum PartyTypes { Contract, Customer, DeliveryAddress, Supplier, User, Contact, Consumable }
        public async Task<List<string>> RecordHistory(CallHistoryEntryContract callresult)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            string _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            List<string> errors = SanityCheck(callresult, _DTS_connectionString);
            if (errors.Count() == 0)
            {
                string[] tableInfo = IdentifyPartyForCall(callresult);
                if (validatePartyForCall(tableInfo, callresult, _DTS_connectionString) != 3)
                {
                    if (CallDoesntExist(callresult, _DTS_connectionString, tableInfo))
                    {
                        _ = DoInsert(callresult, _DTS_connectionString, tableInfo);
                    }
                }
                else
                    errors.Add($"Code : {callresult.PartyCode} was not found within the database");
            }
            return errors;
        }
        public List<string> SanityCheck(CallHistoryEntryContract callHistory, string _DTS_connectionString)
        {
            List<string> result = new List<string>();
            //Basic Checks
            if (callHistory.IncomingCallNumber?.Equals(null) == false)
            {
                if (callHistory.IncomingCallNumber.StartsWith("+27"))
                { callHistory.IncomingCallNumber = string.Concat("0", callHistory.IncomingCallNumber.AsSpan(3)); }
            }
            else
            if (callHistory.IncomingCallNumber?.Equals(null) == false)
                if (!callHistory.IncomingCallNumber.All(char.IsDigit))
                { result.Add("Incoming No Must Be Numeric"); }
            if (callHistory.OutgoingCallNumber?.Equals(null) == false)
            {
                if (callHistory.OutgoingCallNumber.StartsWith("+27"))
                { callHistory.OutgoingCallNumber = string.Concat("0", callHistory.OutgoingCallNumber.AsSpan(3)); }
            }
            if (callHistory.OutgoingCallNumber?.Equals(null) == false)
                if (!callHistory.OutgoingCallNumber.All(char.IsDigit))
                { result.Add("Outgoing No Must Be Numeric"); }
            if (callHistory.ContactFullName?.Equals(null) == false)
                if (callHistory.ContactFullName?.Length > 50)
                { result.Add("Contact full name is limited to 50 characters"); }
            if (callHistory.CallId?.Equals(null) == true)
            { result.Add("CallId Cannot Be Null"); }
            if (callHistory.SipCallId?.Equals(null) == true)
            { result.Add("SIPCallid Cannot Be Null"); }
            if (callHistory.Extension?.Equals(null) == true)
            { result.Add("Extension Cannot Be Null"); }
            if (callHistory.Username?.Equals(null) == true)
            { result.Add("User Name Cannot Be Null"); }
            else 
            {
                using (var connection = new OdbcConnection(_DTS_connectionString))
                {
                    try
                    {
                        connection.Open();
                        string sql = "SELECT [User Name] FROM [User] WHERE [User Name] = '" + callHistory.Username + "'";
                        var command = new OdbcCommand(sql, connection);
                        var reader = command.ExecuteReader();
                        if (!reader.HasRows)
                        { result.Add($"User {callHistory.Username} was not found in the database"); }
                    }
                    catch (OdbcException ex)
                    {
                        throw ex;
                    }
                }
            }
            return result;
        }
        private bool CallDoesntExist(CallHistoryEntryContract callresult, string _DTS_connectionString, string[] tableInfo)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT * "
                                + "FROM [Call Result Log] "
                                + "WHERE [PBX Unique ID] = '" + callresult.CallId + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        using (var connection1 = new OdbcConnection(_DTS_connectionString))
                        {
                            try
                            {
                                connection1.Open();
                                string sql1 = "UPDATE [Call Result Log] "
                                           + "	  SET [Source] = '" + callresult.Source + "', "
                                           + "	  	  [Call Batch Date] = '" + DateTime.Now + "', "
                                           + "	  	  [Reference Code] = '" + callresult.PartyCode + "', "
                                           + "	  	  [Reference Type] = 0, "
                                           + "	  	  [Contact Name] = '" + callresult.ContactFullName + "', "
                                           + "	  	  [Contact Number] = '" + callresult.IncomingCallNumber + "' "
                                           + "WHERE [PBX Unique ID] = '" + callresult.CallId + "' ";
                                var command1 = new OdbcCommand(sql1, connection1);
                                int rows = command1.ExecuteNonQuery();  
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                        return false;
                    }
                    else
                        return true;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        private int DoInsert(CallHistoryEntryContract callresult, string _DTS_connectionString, string[] tableinfo) 
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    DateTime localTime = callresult.UtcStartTime.ToLocalTime();
                    connection.Open();
                    int direction = 0;
                    if (callresult.CallDirection == "Outgoing")
                        direction = 0;
                    else 
                        direction = 1;
                    string sql = "DECLARE @CallID INT " 
                               + "INSERT INTO [Call Result Log] ([Source] "
                               + "							    ,[Call Batch Date] "
                               + "							    ,[Reference Code] "
                               + "							    ,[Reference Type] "
                               + "							    ,[Contact Name] "
                               + "							    ,[Contact Number] "
                               + "							    ,[Start Time] "
                               + "							    ,[End Time] "
                               + "							    ,[Incoming] "
                               + "							    ,[Disposition] "
                               + "							    ,[Result ID] "
                               + "							    ,[Result Specific] "
                               + "							    ,[Action No] "
                               + "							    ,[PBX Unique ID] "
                               + "							    ,[User Name] "
                               + "							    ,[PBX Extension]) "
                               + "SELECT '" + callresult.Source + "', "
                               + "	     '" + DateTime.Now + "', "
                               + "	     '" + callresult.PartyCode + "', "
                               + "	     " + tableinfo[2] + ", "
                               + "	     '" + callresult.ContactFullName + "', "
                               + "	     '" + callresult.IncomingCallNumber + "', "
                               + "	     '" + localTime + "', "
                               + "	     '" + localTime.AddSeconds(callresult.DurationInSeconds) + "', "
                               + "	     " + direction + ", "
                               + "	     'ANSWERED', "
                               + "	     NULL, "
                               + "	     0, "
                               + "	     null,"
                               + "	     '" + callresult.CallId + "',"
                               + "	     '" + callresult.Username + "', "
                               + "	     null "
                               + "SELECT @CallID = SCOPE_IDENTITY() "
                               + "INSERT INTO [Call Result Log External Reference] ([Call ID] "
                               + "												   ,[Reference Code] "
                               + "												   ,[Reference Type]) "
                               + "SELECT @CallID, "
                               + "	      '" + callresult.PartyCode + "',  "
                               + "	      " + tableinfo[2];
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        private string[] IdentifyPartyForCall(CallHistoryEntryContract callHistory)
        {
            if (!Enum.TryParse(callHistory.PartyType, out PartyTypes partyType))
            {
                throw new NotSupportedException($"Party type {callHistory.PartyType} is not supported.");
            }
            string[] result = new string[3];
            switch (partyType)
            {
                case PartyTypes.Contract:
                    result[0] = "Contract";
                    result[1] = "Contract No";
                    result[2] = "2";
                    break;
                case PartyTypes.Customer:
                    result[0] = "Customer";
                    result[1] = "Account No";
                    result[2] = "1";
                    break;
                case PartyTypes.DeliveryAddress:
                    result[0] = "Delivery Address";
                    result[1] = "Delivery Address Code";
                    result[2] = "14";
                    break;
                case PartyTypes.Supplier:
                    result[0] = "Supplier";
                    result[1] = "Supplier No";
                    result[2] = "4";
                    break;
                case PartyTypes.User:
                    result[0] = "User";
                    result[1] = "User Name";
                    result[2] = "3";
                    break;
                case PartyTypes.Contact:
                    result[0] = "Contact";
                    result[1] = "Contact No";
                    result[2] = "5";
                    break;
                case PartyTypes.Consumable:
                    result[0] = "Contract";
                    result[1] = "Contract No";
                    result[2] = "NULL";
                    break;
                default:
                    throw new NotSupportedException($"Party type {partyType} is not supported.");
            }
            return result;
        }
        private int validatePartyForCall(string[] partyTableInfo, CallHistoryEntryContract callHistory, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT * "
                                + "FROM [" + partyTableInfo[0] + "] "
                                + "WHERE [" + partyTableInfo[1] + "] = '" + callHistory.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        return 1;
                    }
                    else
                    {
                        if (callHistory.PartyType == "DeliveryAddress")
                        {
                            using (var connection1 = new OdbcConnection(_DTS_connectionString))
                            {
                                try
                                {
                                    connection1.Open();
                                    string sql1 = "SELECT * "
                                                + "FROM [Supplier Delivery Address] "
                                                + "WHERE [Delivery Address Code] = '" + callHistory.PartyCode + "'";
                                    var command1 = new OdbcCommand(sql, connection1);
                                    var reader1 = command1.ExecuteReader();
                                    if (reader1.HasRows)
                                        return 2;
                                    else
                                        return 3;
                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
                            }
                        }
                        else
                            return 3;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
