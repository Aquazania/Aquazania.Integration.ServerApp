using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterLinkedPartyContract;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedContactParty : ILinkedPartyConvertor
    {
        private string _COM_connectionString;
        private string _DTS_connectionString;
        public LinkedContactParty(IConfiguration configuration)
        {
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }
        public async Task<List<string>> Convert(ChangedLinkedContactContract party)
        {
            int rows = 0;
            List<string> errors = SanityCheck(party);
            if (errors.Count() == 0)
                if (ValidateParty(party) == 1)
                    _ = UpdateRequired(party) > 0;
                else
                {
                    errors.Add("Party Code Was Not Found In Database");
                }
            return errors;
        }
        public int DoInsert(ChangedLinkedContactContract party, int updatetype = 1)
        {
            using (var connection = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "DECLARE @ContactID INT "
                                + "DECLARE @ExternalReferenceID INT "
                                + "SELECT @ExternalReferenceID = MAX(DocumentReferenceID) "
                                + "FROM DocumentReference "
                                + "WHERE DocumentReferenceTypeID = 5 AND "
                                + "      DocumentReferenceCode = '" + party.ParentPartyCode + "' "
                                //Insert into Contact Table
                                + "INSERT INTO [Contact] ([ContactName] "
                                + "					    ,[ContactLastName] "
                                + "					    ,[ExternalReferenceID] "
                                + "					    ,[HasMultipleExternalReferences] "
                                + "					    ,[Datestamp] "
                                + "					    ,[Hostname] "
                                + "					    ,[JobDescription] "
                                + "					    ,[SupplierOrdersContact]) "
                                + "SELECT '" + party.ContactFullName + "', "
                                + "	     NULL, "
                                + "	     @ExternalReferenceID, "
                                + "	     0, "
                                + "	     '" + DateTime.Now + "', "
                                + "	     NULL, "
                                + "	     NULL, "
                                + "	     0 "
                                //Gather ID From Contact
                                + "SELECT @ContactID = SCOPE_IDENTITY() "
                                //Insert into ContactPoint
                                + "INSERT INTO [ContactPoint] ([ContactPointTypeID] "
                                + "						     ,[ContactID] "
                                + "						     ,[ContactPointValue] "
                                + "						     ,[AllowSendFrom]) "
                                + "SELECT 2, "
                                + "	     @ContactID, "
                                + "	     '" + party.PhoneNumber + "', "
                                + "	     1 "
                                //Insert into ContactExternalReference
                                + "INSERT INTO [ContactExternalReference] ([ContactID] "
                                + "									     ,[ExternalReferenceID]) "
                                + "SELECT @ContactID, "
                                + "	     @ExternalReferenceID ";
                    var command = new OdbcCommand(sql, connection);
                    return command.ExecuteNonQuery();
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID)
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
        public List<string> SanityCheck(ChangedLinkedContactContract party)
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
        public int UpdateRequired(ChangedLinkedContactContract party, int updatetype = 1)
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
                                + "		dr.DocumentReferenceTypeID = 5 AND "
                                + $"		dr.DocumentReferenceCode = '{party.ParentPartyCode}' "
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
                                    return PerformUpdate(party, System.Convert.ToInt32(readerContacts["ContactID"].ToString()));
                            }
                            if (party.ContactFullName == readerContacts["ContactName"] + " " + readerContacts["ContactLastName"])
                            {
                                if (party.PhoneNumber == readerContacts["ContactPointValue"].ToString())
                                    return 0;
                                else
                                    return PerformUpdate(party, System.Convert.ToInt32(readerContacts["ContactID"].ToString()));
                            }
                        }
                        return DoInsert(party);
                    }
                    else
                    {
                        return DoInsert(party);
                    }
                }
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
        public int ValidateParty(ChangedLinkedContactContract party)
        {
            using (var connectionDTS = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connectionDTS.Open();
                    string sql = "SELECT [Contact No] FROM [Contact] WHERE [Contact No] = '" + party.ParentPartyCode + "'";
                    var command = new OdbcCommand(sql, connectionDTS);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        using (var connection = new OdbcConnection(_COM_connectionString))
                        {
                            try
                            {
                                connection.Open();
                                string sql_COM = "SELECT DocumentReferenceCode "
                                                + "FROM DocumentReference "
                                                + "WHERE DocumentReferenceTypeID = 5 AND "
                                                + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                                var command_COM = new OdbcCommand(sql_COM, connection);
                                var reader_COM = command_COM.ExecuteReader();
                                if (reader_COM.HasRows)
                                {
                                    return 1;
                                }
                                else
                                {
                                    sql_COM = "INSERT INTO [DocumentReference] ([DocumentReferenceTypeID], "
                                            + "								    [DocumentReferenceCode], "
                                            + "								    [DateStamp], "
                                            + "								    [HostName]) "
                                            + "SELECT 5, "
                                            + "	      '" + party.ParentPartyCode + "', "
                                            + "	      '" + DateTime.Now + "', "
                                            + "	      NULL ";
                                    var command_COM1 = new OdbcCommand(sql_COM, connection);
                                    int rows = command_COM1.ExecuteNonQuery();
                                    return 1;
                                }
                            }
                            catch (OdbcException ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    else
                    {
                        return 3;
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
