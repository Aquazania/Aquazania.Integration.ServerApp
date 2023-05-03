using Aquazania.Telephony.Integration.Enums;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Factory.MasterPartyContract.Impl
{
    public class ConsumableParty : AbsParty
    {
        public override int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, party.User.UserName, _DTS_connectionString);
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Consumables] "
                                + "	SET [" + updatedField + "] = '" + newValue + "' "
                                + "WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
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
                    string sql = "SELECT * FROM [Consumables] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (party.PartyPrimaryContactFullName != reader["Consumables Contact Person"].ToString())
                            rows += PerformUpdate("Consumables Contact Person",
                                                    reader["Consumables Contact Person"].ToString(),
                                                    party.PartyPrimaryContactFullName,
                                                    party, _DTS_connectionString);
                        if (party.PartyPrimaryTelephoneNumber != reader["Tel No For Consumables Contact Person"].ToString())
                            rows += PerformUpdate("Tel No For Consumables Contact Person",
                                                    reader["Tel No For Consumables Contact Person"].ToString(),
                                                    party.PartyPrimaryTelephoneNumber,
                                                    party, _DTS_connectionString);
                        if (party.PartyPrimaryCellNumber != reader["Cell No For Consumables Contact Person"].ToString())
                            rows += PerformUpdate("Cell No For Consumables Contact Person",
                                                    reader["Cell No For Consumables Contact Person"].ToString(),
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
                    string sql = "SELECT [Delivery Address Code] FROM [Consumables] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
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
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string deliveryAddressCode, string userName, string _DTS_connectionString)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "DECLARE @UpdateNo INT "
                                + "INSERT INTO [Update History] ([User Name] "
                                + "							   ,[Requested By] "
                                + "							   ,[Reference Type] "
                                + "							   ,[Key Value] "
                                + "							   ,[Date Stamp]) "
                                + "SELECT '" + userName + "', "
                                + "	     NULL, "
                                + "	     14, "
                                + "	     '" + deliveryAddressCode + "', "
                                + "	     '" + DateTime.Now + "' "
                                + "SELECT @UpdateNo = SCOPE_IDENTITY() "
                                + "INSERT INTO [Update History Detail] ([Column Name], "
                                + "								       [New Value], "
                                + "									   [Old Value], "
                                + "									   [Table Name], "
                                + "									   [Update No]) "
                                + "SELECT '" + updatedField + "', "
                                + "	     '" + newValue + "', "
                                + "	     '" + oldValue + "', "
                                + "	     'Consumables', "
                                + "	     @UpdateNo ";
                    var command = new OdbcCommand(sql, connection);
                    command.ExecuteNonQuery();
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
            if (party.ParentPartyCode?.Equals(null) == true)
            { result.Add("Parent Party Code Must Not Be Null"); }
            if (party.ParentPartyType?.Equals(null) == true)
            { result.Add("Consumables Must Have a Parent. Missing Type"); }
            else
            {
                if (party.ParentPartyType != "DeliveryAddress")
                { result.Add("Invalid Parent Party Type. Consumables May Only Have Delivery Addresses As Parents."); }
            }
            return result;
        }
    }
}
