using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterLinkedPartyContract;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedDeliveryAddressParty : ILinkedPartyConvertor
    {
        private string _COM_connectionString;
        private string _DTS_connectionString;
        public LinkedDeliveryAddressParty(IConfiguration configuration)
        {
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }
        public int Convert(ChangedLinkedContactContract party)
        {
            int rows = 0;
            int updatetype = ValidateParty(party);
            if (updatetype == 1)
            {
                rows += UpdateRequired(party, updatetype);
            }
            else
            {
                if (updatetype == 2)
                {
                    rows += UpdateRequired(party, updatetype);
                }else 
                    throw new KeyNotFoundException($"Code : {party.ParentPartyCode} was not found within the database");
            }
            return rows;
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
                               + "WHERE DocumentReferenceTypeID = 14 AND "
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

        public int UpdateRequired(ChangedLinkedContactContract party, int updatetype = 1)
        {
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
                               + "		dr.DocumentReferenceTypeID = 14 AND "
                               + $"		dr.DocumentReferenceCode = '{party.ParentPartyCode}' "
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

        public int ValidateParty(ChangedLinkedContactContract party)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT [Delivery Address Code] FROM [Delivery Address] WHERE [Delivery Address Code] = '" + party.ParentPartyCode + "'";
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
                                               + "WHERE DocumentReferenceTypeID = 14 AND "
                                               + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                                var command_COM = new OdbcCommand(sql_COM, connection_COM);
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
                                            + "SELECT 14, "
                                            + "	      '" + party.ParentPartyCode + "', "
                                            + "	      '" + DateTime.Now + "', "
                                            + "	      NULL ";
                                    var command_COM1 = new OdbcCommand(sql_COM, connection_COM);
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
                        using (var connection_Supplier = new OdbcConnection(_DTS_connectionString))
                        {
                            try
                            {
                                connection_Supplier.Open();
                                string sql_Supplier = "SELECT [Delivery Address Code] FROM [Supplier Delivery Address] WHERE [Delivery Address Code] = '" + party.ParentPartyCode + "'";
                                var command_Supplier = new OdbcCommand(sql_Supplier, connection_Supplier);
                                var reader_Supplier = command_Supplier.ExecuteReader();
                                if (reader_Supplier.HasRows)
                                {
                                    using (var connection_COM = new OdbcConnection(_COM_connectionString))
                                    {
                                        try
                                        {
                                            connection_COM.Open();
                                            string sql_COM = "SELECT DocumentReferenceCode "
                                                           + "FROM DocumentReference "
                                                           + "WHERE DocumentReferenceTypeID = 16 AND "
                                                           + "		DocumentReferenceCode = '" + party.ParentPartyCode + "'";
                                            var command_COM = new OdbcCommand(sql_COM, connection_COM);
                                            var reader_COM = command_COM.ExecuteReader();
                                            if (reader_COM.HasRows)
                                            {
                                                return 2;
                                            }
                                            else
                                            {
                                                sql_COM = "INSERT INTO [DocumentReference] ([DocumentReferenceTypeID], "
                                                        + "								    [DocumentReferenceCode], "
                                                        + "								    [DateStamp], "
                                                        + "								    [HostName]) "
                                                        + "SELECT 16, "
                                                        + "	      '" + party.ParentPartyCode + "', "
                                                        + "	      '" + DateTime.Now + "', "
                                                        + "	      NULL ";
                                                var command_COM1 = new OdbcCommand(sql_COM, connection_COM);
                                                int rows = command_COM1.ExecuteNonQuery();
                                                return 2;
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
                catch (OdbcException ex)
                {
                    throw ex;
                }
            }
        }
    }
}
