﻿using Aquazania.Telephony.Integration.Models;

namespace HTTPServer.Factory.MasterLinkedPartyContract.Impl
{
    public class LinkedContractParty : ILinkedPartyConvertor
    {
        private string _COM_connectionString;
        private string _DTS_connectionString;
        public LinkedContractParty(IConfiguration configuration)
        {
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
        }
        public int Convert(ChangedLinkedContactContract party)
        {
            throw new NotImplementedException();
        }

        public int DoInsert(ChangedLinkedContactContract party)
        {   
            throw new NotImplementedException();
        }

        public int PerformUpdate(ChangedLinkedContactContract party, int ContactID)
        {
            throw new NotImplementedException();
        }

        public int UpdateRequired(ChangedLinkedContactContract party)
        {
            throw new NotImplementedException();
        }

        public bool ValidateParty(ChangedLinkedContactContract party)
        {
            throw new NotImplementedException();
        }
    }
}
