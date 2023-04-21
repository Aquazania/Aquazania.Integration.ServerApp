using Aquazania.Telephony.Integration.Models;
using System.Data.Common;
using System.Data.Odbc;
using System.Net;
using System.Net.Mail;
using System.Security.Principal;
using System.Xml.Linq;

namespace HTTPServer.Factory.MasterPartyContract.Impl
{
    public class CustomerParty : IPartyConvertor
    {
        public CustomerParty(IConfiguration configuration)
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
                    string sql = "SELECT [Account No] FROM [Customer] WHERE [Account No] = '" + party.PartyCode + "'";
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
                    string sql = "SELECT * FROM [Customer] WHERE [Account No] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        if (party.PartyPrimaryContactFullName != reader["Creditors Clerk"].ToString())
                            rows += PerformUpdate("Creditors Clerk",
                                                    reader["Creditors Clerk"].ToString(),
                                                    party.PartyPrimaryContactFullName,
                                                    party);
                        if (party.PartyPrimaryTelephoneNumber != reader["Telephone No"].ToString())
                            rows += PerformUpdate("Telephone No",
                                                    reader["Telephone No"].ToString(),
                                                    party.PartyPrimaryTelephoneNumber,
                                                    party);
                        if (party.PartyPrimaryCellNumber != reader["Cell Phone No"].ToString())
                            rows += PerformUpdate("Cell Phone No",
                                                    reader["Cell Phone No"].ToString(),
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
            EnterHistoryRecord(updatedField, oldValue, newValue, party.PartyCode, party.User.UserName);
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [Customer] "
                                + "	SET [" + updatedField + "] = '" + newValue + "' "
                                + "WHERE [Account No] = '" + party.PartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string accountNo, string userName)
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
                                + "	     1, "
                                + "	     '" + accountNo + "', "
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
                                + "	     'Customer', "
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
            if (party.ParentPartyCode?.Equals(null) == false)
            { result.Add("Customers Not Must Have a Parent. Code Provided"); }
            if (party.ParentPartyType?.Equals(null) == false)
            { result.Add("Customers Not Must Have a Parent. Type Provided"); }
            return result;
        }
    }
}
