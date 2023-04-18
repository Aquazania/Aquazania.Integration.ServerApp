﻿using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client;
using System.Data.Odbc;

namespace Aquazania.Integration.ServerApp.Client
{
    public interface IMasterParty
    {
        public void SendMasterParty(ITimed_Client _httpClient, string _DTS_connectionString);
        public void UpdateSyncMasterTable(OdbcConnection connection, OdbcTransaction transaction);
        public List<MasterOwnedPartyContract> buildMasterObject(OdbcConnection connection, OdbcTransaction transaction);
    }
}