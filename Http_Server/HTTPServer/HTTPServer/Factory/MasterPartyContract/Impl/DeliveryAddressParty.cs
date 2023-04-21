using Aquazania.Telephony.Integration.Models;
using Microsoft.Extensions.Configuration;
using System.Data.Odbc;
using System.IO;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class DeliveryAddressParty : IPartyConvertor
    {
        public DeliveryAddressParty(IConfiguration configuration)
        {
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }
        private string _DTS_connectionString;
        public async Task<List<string>> Convert(ChangedPartyContactContract party)
        {
            int rows = 0;
            List<string> errors = SanityCheck(party);
            if (errors.Count() == 0)
                if (ValidateParty(party))
                    _ = UpdateRequired(party) > 0;
                else
                {
                    errors.Add("Party Code Was Not Found In Database");
                }
            return errors;
        }
        public bool ValidateParty(ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "";
                    if (party.ParentPartyType == "Supplier")
                        sql = "SELECT [Delivery Address Code] FROM [Supplier Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    else
                        sql = "SELECT [Delivery Address Code] FROM [Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
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
        public int UpdateRequired(ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    int rows = 0;
                    string sql = "";
                    if (party.ParentPartyType == "Supplier")
                        sql = "SELECT * FROM [Supplier Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    else
                        sql = "SELECT * FROM [Delivery Address] WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (party.PartyPrimaryContactFullName != reader["Contact Person"].ToString())
                            rows += PerformUpdate("Contact Person",
                                                    reader["Contact Person"].ToString(),
                                                    party.PartyPrimaryContactFullName,
                                                    party);
                        if (party.PartyPrimaryTelephoneNumber != reader["Tel No For Contact Person"].ToString())
                            rows += PerformUpdate("Tel No For Contact Person",
                                                    reader["Tel No For Contact Person"].ToString(),
                                                    party.PartyPrimaryTelephoneNumber,
                                                    party);
                        if (party.PartyPrimaryCellNumber != reader["Cell No For Contact Person"].ToString())
                            rows += PerformUpdate("Cell No For Contact Person",
                                                    reader["Cell No For Contact Person"].ToString(),
                                                    party.PartyPrimaryCellNumber,
                                                    party);
                    }
                    return rows;
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public int PerformUpdate(string updatedField, string oldValue, string newValue, ChangedPartyContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "";
                    if (party.ParentPartyType == "Supplier")
                    {
                        EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, 44, "Supplier Delivery Address", party.User.UserName);
                        sql = "UPDATE [Supplier Delivery Address] "
                            + "	SET [" + updatedField + "] = '" + newValue + "' "
                            + "WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    }
                    else
                    {
                        EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, 14, "Delivery Address", party.User.UserName);
                        sql = "UPDATE [Delivery Address] "
                            + "	SET [" + updatedField + "] = '" + newValue + "' "
                            + "WHERE [Delivery Address Code] = '" + party.PartyCode + "'";
                    }
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string deliveryAddressCode, int referenceType, string tableName, string userName)
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
                                + "	     " + referenceType + ", "
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
                                + "	     '" + tableName + "', "
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
        public List<string> SanityCheck(ChangedPartyContactContract party)
        {
            List<string> result = new List<string>();
            //Basic Checks
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
            //Situational Checks
            if (party.ParentPartyCode?.Equals(null) == true)
            { result.Add("Parent Party Code Must Not Be Null"); }
            if (party.ParentPartyType?.Equals(null) == true)
            { result.Add("Delivery Addresses Must Have a Parent"); }
            if (party.ParentPartyType?.Equals(null) == false & party.ParentPartyCode?.Equals(null) == false)
                if (!(party.ParentPartyType == "Customer" || party.ParentPartyType == "Supplier"))
                { result.Add("Invalid Parent Party Type. Delivery Addresses May Only Have Suppliers Or Customers."); }
            return result;
        }
    }
}
