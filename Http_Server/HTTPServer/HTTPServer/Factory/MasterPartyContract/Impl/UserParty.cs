using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.Net.Http;
using System;
using Aquazania.Integration.ServerApp.Factory.MasterPartyContract;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class UserParty : AbsParty
    {
        public override int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [User] "
                                + "	SET [" + updatedField + "] = '" + newValue + "' "
                                + "WHERE [User Name] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public override int UpdateRequired(ChangedPartyContactContract party, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    int rows = 0;
                    string sql = "SELECT * FROM [User] WHERE [User Name] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection); 
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (party.PartyPrimaryTelephoneNumber != reader["Telephone No"].ToString())
                            rows += PerformUpdate("Telephone No",
                                                    reader["Telephone No"].ToString(),
                                                    party.PartyPrimaryTelephoneNumber,
                                                    party, _DTS_connectionString);
                        if (party.PartyPrimaryCellNumber != reader["Cell Phone No"].ToString())
                            rows += PerformUpdate("Cell Phone No",
                                                    reader["Cell Phone No"].ToString(),
                                                    party.PartyPrimaryCellNumber,
                                                    party, _DTS_connectionString);
                    }
                    return rows;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public override bool ValidateParty(ChangedPartyContactContract party, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT [User Name] FROM [User] WHERE [User Name] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public override List<string> SanityCheck(ChangedPartyContactContract party, string _DTS_connectionString)
        {
            List<string> result = new List<string>();
            //Basic Checks
            if (party.PartyPrimaryCellNumber?.Equals(null) == false)
                if (party.PartyPrimaryCellNumber.StartsWith("+27"))
                    party.PartyPrimaryCellNumber = string.Concat("0", party.PartyPrimaryCellNumber.AsSpan(3));
            if (party.PartyPrimaryTelephoneNumber?.Equals(null) == false)
                if (party.PartyPrimaryTelephoneNumber.StartsWith("+27"))
                    party.PartyPrimaryTelephoneNumber = string.Concat("0", party.PartyPrimaryCellNumber.AsSpan(3));
            if (party.PartyPrimaryContactFullName?.Equals(null) == true)
            { result.Add("Contact Full Name Cannot Be Null"); }
            if (party.PartyPrimaryCellNumber?.Equals(null) == false)
                if (!party.PartyPrimaryCellNumber.All(char.IsDigit))
                { result.Add("Cell No Must Be Numeric"); }
            if (party.PartyPrimaryTelephoneNumber?.Equals(null) == false)
                if (!party.PartyPrimaryTelephoneNumber.All(char.IsDigit))
                { result.Add("Telephone No Must Be Numeric"); }
            if (party.PartyPrimaryTelephoneNumber?.Equals(null) == true & party.PartyPrimaryCellNumber?.Equals(null) == true)
            { result.Add("At Least One Contact No Must Be Provided"); }
            if (party.PartyCode?.Equals(null) == true)
            { result.Add("Party Code Cannot Be Null"); }
            if (party.User.UserName?.Equals(null) == true)
            { result.Add("User Name Cannot Be Null"); }
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT [User Name] FROM [User] WHERE [User Name] = '" + party.User.UserName + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (!reader.HasRows)
                    { result.Add($"User {party.User.UserName} was not found in the database"); }
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
            //Situational Checks
            if (!party.ParentPartyType?.Equals(null) == true)
            {
                if (party.ParentPartyType != "Customer")
                { result.Add("Invalid Parent Party Type. Users May Only Have Customers As Parents."); }
                if (party.ParentPartyCode?.Equals(null) == true)
                { result.Add("You Have Provided a type but no code."); }
            }
            if (!party.ParentPartyCode?.Equals(null) == true)
                if (party.ParentPartyType?.Equals(null) == true)
                { result.Add("You have provided a code but no type"); }
            return result;
        }
    }
}
