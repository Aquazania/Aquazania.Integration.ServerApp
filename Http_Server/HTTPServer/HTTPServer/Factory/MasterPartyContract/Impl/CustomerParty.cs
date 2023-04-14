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
        public int Convert(ChangedPartyContactContract party)
        {
            int rows = 0;
            if (ValidateParty(party))
            {
                rows += UpdateRequired(party);
            }
            else
            {
                throw new KeyNotFoundException($"Code : {party.PartyCode} was not found within the database");
            }
            return rows;
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
                    int rows = 0;
                    connection.Open();
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

            #region first time
            #region commentted
            //var contactNumbers = new List<Dictionary<string, object>>();
            //using (var connectionCommunicator = new OdbcConnection(_COM_connectionString))
            //{
            //    try 
            //    {
            //        connectionCommunicator.Open();
            //        string sql = "SELECT  v.ContactPointValue, "
            //                   + "		  v.ContactPointTypeID, "
            //                   + "		  cpt.[Description], "
            //                   + "		  max(v.ContactID) ContactID, "
            //                   + "		  v.ContactName, "
            //                   + "		  v.ContactLastName, "
            //                   + "		  v.JobDescription, "
            //                   + "		  v.SupplierOrdersContact "
            //                   + "FROM  viewContactDocumentReference V "
            //                   + "	INNER JOIN ContactPointType cpt on "
            //                   + "		cpt.ContactPointTypeID = v.ContactPointTypeID "
            //                   + "	INNER JOIN	DocumentReference dr ON "
            //                   + "		v.ExternalReferenceID = dr.DocumentReferenceID AND "
            //                   //DocumentReferenceTypeID 1 is for customers
            //                   + "		dr.DocumentReferenceTypeID = 1 AND "
            //                   + $"		dr.DocumentReferenceCode = '{party.Code}' "
            //                   // Contact point 2 is for cell phone numbers, 1 is for emails
            //                   + "WHERE v.ContactPointTypeID = isnull(2, v.ContactPointTypeID) "
            //                   + "GROUP BY v.ContactPointValue, "
            //                   + "		   v.ContactPointTypeID, "
            //                   + "		   cpt.[Description], "
            //                   + "		   v.ContactName, "
            //                   + "		   v.ContactLastName, "
            //                   + "		   v.JobDescription, "
            //                   + "		   v.SupplierOrdersContact ";
            //        var commandContacts = new OdbcCommand(sql, connectionCommunicator);
            //        var readerContacts = commandContacts.ExecuteReader();
            //        if (readerContacts != null) 
            //        {
            //            while (readerContacts.Read())
            //            {
            //                var contact = new Dictionary<string, object>()
            //                {
            //                    {"value", readerContacts["ContactPointValue"].ToString() },
            //                    {"IsPrimary", false  },
            //                    {"IsActive", true  }
            //                };
            //                contactNumbers.Add(contact);
            //            }
            //        }

            //    }
            //    catch (OdbcException ex)
            //    {
            //        throw ex;
            //    }
            //}
            #endregion

            //using (var connectionDTS = new OdbcConnection(_DTS_connectionString))
            //{
            //    try
            //    {
            //        connectionDTS.Open();

            //        var deliveryAddresses = new List<Dictionary<string, object>>();
            //        Dictionary<string, object> response = null;
            //        string sql = "SELECT T2.[Account Name], T2.[Account No], T1.[Delivery Address Line 2], T1.[Delivery Address Line 3],"
            //                   + "       T1.[Delivery Address Code], T1.[Tel No For Contact Person], T1.[Cell No For Contact Person] "
            //                   + "FROM [Delivery Address] T1"
            //                   + "   INNER JOIN [Customer] T2 ON "
            //                   + "     T1.[Account No] = T2.[Account No] "
            //                   + $"WHERE T1.[Account No] = '{party.Code}'";
            //        var commandDac = new OdbcCommand(sql, connectionDTS);
            //        var readerDac = commandDac.ExecuteReader();
            //        if (readerDac != null)
            //        {
            //            while (readerDac.Read())
            //            {
            //                if (!String.IsNullOrEmpty(readerDac["Tel No For Contact Person"].ToString()))
            //                {
            //                    var contact = new Dictionary<string, object>()
            //                    {
            //                        {"value", readerDac["Tel No For Contact Person"].ToString() },
            //                        {"IsPrimary", true  },
            //                        {"IsActive", true  }
            //                    };
            //                    contactNumbers.Add(contact);
            //                };
            //                if (!String.IsNullOrEmpty(readerDac["Cell No For Contact Person"].ToString()))
            //                {
            //                    var contact = new Dictionary<string, object>()
            //                    {
            //                        {"value", readerDac["Cell No For Contact Person"].ToString() },
            //                        {"IsPrimary", true  },
            //                        {"IsActive", true  }
            //                    };
            //                    contactNumbers.Add(contact);
            //                };
            //                var address = new Dictionary<string, object>
            //                {
            //                    { "Code", readerDac["Delivery Address Code"].ToString() },
            //                    { "AccountCode", readerDac["Account No"].ToString()},
            //                    { "AccountName", readerDac["Account Name"]},
            //                    { "LinkName", readerDac["Account Name"].ToString() + ": "
            //                                + readerDac["Delivery Address Line 2"].ToString() + ", "
            //                                + readerDac["Delivery Address Line 3"].ToString()},
            //                    // Use enum for linktype
            //                    { "LinkType", "Party" },
            //                    { "IsPrimary", false}
            //                };
            //                deliveryAddresses.Add(address);
            //            }
            //        }
            //        else
            //        {
            //            deliveryAddresses = null;
            //        }

            //        var commandConsumable = new OdbcCommand($"SELECT * FROM [Consumables] WHERE [Account No] = '{party.Code}'", connectionDTS);
            //        var readerConsumable = commandConsumable.ExecuteReader();
            //        if (readerConsumable != null)
            //        {
            //            while (readerConsumable.Read())
            //            {
            //                if (!String.IsNullOrEmpty(readerConsumable["Tel No For Consumables Contact Person"].ToString()))
            //                {
            //                    var contact = new Dictionary<string, object>()
            //                     {
            //                        {"value", readerConsumable["Tel No For Consumables Contact Person"].ToString() },
            //                        {"IsPrimary", true  },
            //                        {"IsActive", true  }
            //                    };
            //                    contactNumbers.Add(contact);
            //                }
            //                if (!String.IsNullOrEmpty(readerConsumable["Cell No For Consumables Contact Person"].ToString()))
            //                {
            //                    var contact = new Dictionary<string, object>()
            //                    {
            //                        {"value", readerConsumable["Cell No For Consumables Contact Person"].ToString() },
            //                        {"IsPrimary", true  },
            //                        {"IsActive", true  }
            //                    };
            //                    contactNumbers.Add(contact);
            //                }
            //            }
            //        }

            //        var commandAcc = new OdbcCommand($"SELECT * FROM Customer WHERE [Account No] = '{party.Code}'", connectionDTS);
            //        var readerAcc = commandAcc.ExecuteReader();
            //        if (readerAcc != null)
            //        {
            //            while (readerAcc.Read())
            //            {
            //                if (!String.IsNullOrEmpty(readerAcc["Telephone No"].ToString()))
            //                {
            //                    var contact = new Dictionary<string, object>()
            //                    {
            //                        {"value", readerAcc["Telephone No"].ToString() },
            //                        {"IsPrimary", true  },
            //                        {"IsActive", true  }
            //                    };
            //                    contactNumbers.Add(contact);
            //                };
            //                if (!String.IsNullOrEmpty(readerAcc["Cell Phone No"].ToString()))
            //                {
            //                    var contact2 = new Dictionary<string, object>()
            //                    {
            //                        {"value", readerAcc["Cell Phone No"].ToString() },
            //                        {"IsPrimary", true  },
            //                        {"IsActive", true  }
            //                    };
            //                    contactNumbers.Add(contact2);
            //                }
            //                response = new Dictionary<string, object>
            //                {
            //                    { "Code", readerAcc["Account No"].ToString() },
            //                    { "AccountCode", readerAcc["Account No"].ToString() },
            //                    { "AccountName", readerAcc["Account Name"].ToString() },
            //                    { "FullName", null},
            //                    { "PartyType", "Customer" },
            //                    { "Contact Numbers", contactNumbers },
            //                    { "Relationships", deliveryAddresses}
            //                };
            //            }
            //            return response;
            //        }
            //        else
            //        {
            //            throw new KeyNotFoundException($"Code : {party.Code} was not found within the database");
            //        }
            //    }
            //    catch (OdbcException ex)
            //    {
            //        throw ex;
            //    }
            //}
            #endregion

        }
        public void EnterHistoryRecord(string updatedField, string oldValue, string newValue, string accountNo, string userName)
        {
            using (var connection = new OdbcConnection(_DTS_connectionString))
            {
                try
                {
                    //Swap this around. take the update no from history and put this into one query and execute both 
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
    }
}
