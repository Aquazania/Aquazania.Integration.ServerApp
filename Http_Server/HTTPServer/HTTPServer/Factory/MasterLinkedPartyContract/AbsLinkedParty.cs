using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterLinkedPartyContract;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Factory.MasterLinkedPartyContract
{
    public abstract class AbsLinkedParty : ILinkedPartyConvertor
    {
        public async Task<List<string>> Convert(ChangedLinkedContactContract party, IConfiguration configuration)
        {
            string _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            string _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");

            int rows = 0;
            List<string> errors = SanityCheck(party, _DTS_connectionString);
            if (errors.Count() == 0)
                if (ValidateParty(party, _COM_connectionString, _DTS_connectionString) == 1)
                    _ = UpdateRequired(party, _COM_connectionString) > 0;
                else
                {
                    errors.Add("Party Code Was Not Found In Database");
                }
            return errors;
        }
        public List<string> SanityCheck(ChangedLinkedContactContract party, string _DTS_connectionString)
        {
            List<string> result = new List<string>();
            //Basic Checks
            if (party.ParentPartyCode?.Equals(null) == true)
            { result.Add("Parent Party Code Must Not Be Null"); }
            if (party.ContactFullName?.Equals(null) == true)
            { result.Add("Contact Full Name Must Not Be Null"); }
            if (party.PhoneNumber?.Equals(null) == true)
            { result.Add("Phone No Must Not Be Null"); }
            else if (party.PhoneNumber.StartsWith("+27"))
            {
                party.PhoneNumber = string.Concat("0", party.PhoneNumber.AsSpan(3));
                if (!party.PhoneNumber.All(char.IsDigit))
                { result.Add("Phone Number Must Be Digits"); }
            }
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
            return result;
        }
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID, string _COM_connectionString)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "UPDATE [ContactPoint] "
                                + "  SET [ContactPointValue] = '" + party.PhoneNumber + "' "
                                + "WHERE [ContactID] = " + ContactID;
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public virtual int UpdateRequired(ChangedLinkedContactContract party, string _COM_connectionString, int updatetype = 1)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT  v.ContactPointValue, "
                                + "		  v.ContactPointTypeID, "
                                + "		  cpt.[Description], "
                                + "		  max(v.ContactID) ContactID, "
                                + "		  v.ContactName, "
                                + "		  v.ContactLastName, "
                                + "		  v.JobDescription, "
                                + "		  v.SupplierOrdersContact "
                                + "FROM  viewContactDocumentReference V "
                                + "	INNER JOIN ContactPointType cpt on "
                                + "		cpt.ContactPointTypeID = v.ContactPointTypeID "
                                + "	INNER JOIN	DocumentReference dr ON "
                                + "		v.ExternalReferenceID = dr.DocumentReferenceID AND "
                                //DocumentReferenceTypeID 1 is for customers
                                + "		dr.DocumentReferenceTypeID = 1 AND "
                                + $"		dr.DocumentReferenceCode = '{party.ParentPartyCode}' "
                                // Contact point 2 is for cell phone numbers, 1 is for emails
                                + "WHERE v.ContactPointTypeID = isnull(2, v.ContactPointTypeID) "
                                + "GROUP BY v.ContactPointValue, "
                                + "		   v.ContactPointTypeID, "
                                + "		   cpt.[Description], "
                                + "		   v.ContactName, "
                                + "		   v.ContactLastName, "
                                + "		   v.JobDescription, "
                                + "		   v.SupplierOrdersContact ";
                    var commandContacts = new OdbcCommand(sql, connection);
                    var readerContacts = commandContacts.ExecuteReader();
                    if (readerContacts.HasRows)
                    {
                        while (readerContacts.Read())
                        {
                            if (party.ContactFullName == readerContacts["ContactName"].ToString())
                            {
                                if (party.PhoneNumber == readerContacts["ContactPointValue"].ToString())
                                    return 0;
                                else
                                    return PerformUpdate(party, System.Convert.ToInt32(readerContacts["ContactID"].ToString()), _COM_connectionString);
                            }
                            if (party.ContactFullName == readerContacts["ContactName"] + " " + readerContacts["ContactLastName"])
                            {
                                if (party.PhoneNumber == readerContacts["ContactPointValue"].ToString())
                                    return 0;
                                else
                                    return PerformUpdate(party, System.Convert.ToInt32(readerContacts["ContactID"].ToString()), _COM_connectionString);
                            }
                        }
                        return DoInsert(party, _COM_connectionString);
                    }
                    else
                    {
                        return DoInsert(party, _COM_connectionString);
                    }
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public abstract int DoInsert(ChangedLinkedContactContract party, string _COM_connectionString, int updatetype = 1);
        public abstract int ValidateParty(ChangedLinkedContactContract party, string _COM_connectionString, string _DTS_connectionString);
    }
}
