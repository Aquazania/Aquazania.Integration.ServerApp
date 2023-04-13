using Aquazania.Telephony.Integration.Models;
using System.Data.Odbc;
using System.Reflection.Metadata.Ecma335;

namespace HTTPServer.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedCustomerParty : ILinkedPartyConvertor
    {
        private string _COM_connectionString;
        private string _DTS_connectionString;
        public LinkedCustomerParty(IConfiguration configuration)
        {
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }

        public int Convert(ChangedLinkedContactContract party)
        {
            int rows = 0;
            if (ValidateParty(party))
            {
                rows += UpdateRequired(party);
            }
            else
            {
                throw new KeyNotFoundException($"Code : {party.ParentPartyCode} was not found within the database");
            }
            return rows;
        }

        public int DoInsert(ChangedLinkedContactContract party)
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
                               + "WHERE DocumentReferenceTypeID = 1 AND "
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
                               + "	   @ContactID, "
                               + "	   '" + party.PhoneNumber + "', "
                               + "	   1 "
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

        public int UpdateRequired(ChangedLinkedContactContract party)
        {
            var contactNumbers = new List<Dictionary<string, object>>();
            using (var connectionCommunicator = new OdbcConnection(_COM_connectionString))
            {
                try
                {
                    connectionCommunicator.Open();
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
                    var commandContacts = new OdbcCommand(sql, connectionCommunicator);
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

        public bool ValidateParty(ChangedLinkedContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT [Account No] FROM [Customer] WHERE [Account No] = '" + party.ParentPartyCode + "'";
                    var command = new OdbcCommand(sql, connection);
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        using (var connection_COM = new OdbcConnection(_COM_connectionString))
                        {
                            try
                            {
                                connection_COM.Open();
                                string sql_COM = "SELECT DocumentReferenceCode "
                                               + "FROM DocumentReference "
                                               + "WHERE DocumentReferenceTypeID = 1 AND "
                                               + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                                var command_COM = new OdbcCommand(sql_COM, connection_COM);
                                var reader_COM = command_COM.ExecuteReader();
                                if (reader_COM.HasRows)
                                {
                                    return true;
                                }
                                else
                                {
                                    sql_COM = "INSERT INTO [DocumentReference] ([DocumentReferenceTypeID], "
                                            + "								    [DocumentReferenceCode], "
                                            + "								    [DateStamp], "
                                            + "								    [HostName]) "
                                            + "SELECT 1, "
                                            + "	      '" + party.ParentPartyCode + "', "
                                            + "	      '" + DateTime.Now + "', "
                                            + "	      NULL ";
                                    var command_COM1 = new OdbcCommand(sql_COM, connection_COM);
                                    int rows = command_COM1.ExecuteNonQuery();
                                    return true;
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
                        return false;
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
