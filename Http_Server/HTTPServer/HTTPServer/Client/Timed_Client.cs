using Aquazania.Integration.ServerApp.Client.Consumable;
using Aquazania.Integration.ServerApp.Client.Contact;
using Aquazania.Integration.ServerApp.Client.Contract;
using Aquazania.Integration.ServerApp.Client.DeliveryAddress;
using Aquazania.Integration.ServerApp.Client.Supplier;
using Aquazania.Integration.ServerApp.Client.SupplierDeliveryAddress;
using Aquazania.Integration.ServerApp.Client.User;
using Aquazania.Telephony.Integration.Models;
using HTTPServer.Client.Customer;
using Newtonsoft.Json;
using System.Data.Odbc;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Text;

namespace HTTPServer.Client
{
    public class Timed_Client 
    {
        private Timer _timer;

        private readonly ITimed_Client _httpClient;
        private string _DTS_connectionString;
        private string _COM_connectionString;
        private string _darielURL;
        private string _darielURLContact;
        public Timed_Client(ITimed_Client timed_client)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _httpClient = timed_client;
            _DTS_connectionString = configuration.GetConnectionString("DTS_Connection");
            _COM_connectionString = configuration.GetConnectionString("Communicator_Connection");
            _darielURL = configuration.GetSection("darielURL").Value;
            _darielURLContact = configuration.GetSection("darielURLContact").Value;
        }

        public void StartTimer()
        {
            _timer = new Timer(CallBackFunctions, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        }

        private void CallBackFunctions(object state)
        {
            MasterContactParty contactmaster = new MasterContactParty(_darielURL);
            contactmaster.SendMasterContactParty(_httpClient, _DTS_connectionString);
            MasterContactLinkedParty contactlinkedparty = new MasterContactLinkedParty(_darielURLContact);
            contactlinkedparty.SendMasterContactLinkedParty(_httpClient, _COM_connectionString);

            MasterContractParty contractmaster = new MasterContractParty(_darielURL);
            contractmaster.SendMasterContractParty(_httpClient, _DTS_connectionString);
            MasterContractLinkedParty contractlinkedparty = new MasterContractLinkedParty(_darielURLContact);
            contractlinkedparty.SendMasterContractLinkedParty(_httpClient, _COM_connectionString);  

            MasterCustomerParty customermaster = new MasterCustomerParty(_darielURL); 
            customermaster.SendMasterCustomerParty(_httpClient, _DTS_connectionString);
            MasterCustomerLinkedParty customerlinkedParty = new MasterCustomerLinkedParty(_darielURLContact);
            customerlinkedParty.SendMasterCustomerLinkedParty(_httpClient, _COM_connectionString);

            MasterDeliveryAddressParty deliveryAddressMaster = new MasterDeliveryAddressParty(_darielURL);
            deliveryAddressMaster.SendMasterDeliveryAddressParty(_httpClient, _DTS_connectionString);
            MasterDeliveryAddressLinkedParty deliveryAddressLinkedMaster = new MasterDeliveryAddressLinkedParty(_darielURLContact); 
            deliveryAddressLinkedMaster.SendMasterDeliveryAddressLinkedParty(_httpClient, _COM_connectionString);

            MasterSupplierParty supplierMaster = new MasterSupplierParty(_darielURL);
            supplierMaster.SendMasterSupplierParty(_httpClient, _DTS_connectionString);
            MasterSupplierLinkedParty supplierLinkedParty = new MasterSupplierLinkedParty(_darielURLContact);
            supplierLinkedParty.SendMasterSupplierLinkedParty(_httpClient, _COM_connectionString);

            MasterSupplierDeliveryAddressParty masterSupplierDeliveryAddress = new MasterSupplierDeliveryAddressParty(_darielURL);
            masterSupplierDeliveryAddress.SendMasterDeliveryAddressParty( _httpClient, _DTS_connectionString);
            MasterSupplierDeliveryAddressLinkedParty masterSupplierDeliveryAddressLinkedParty = new MasterSupplierDeliveryAddressLinkedParty(_darielURLContact); 
            masterSupplierDeliveryAddressLinkedParty.SendMasterDeliveryAddressLinkedParty( _httpClient, _COM_connectionString);

            MasterUserParty masterUserParty = new MasterUserParty(_darielURL);
            masterUserParty.SendMasterUserParty( _httpClient, _DTS_connectionString);
            MasterUserLinkedParty masterUserLinkedParty = new MasterUserLinkedParty(_darielURLContact);
            masterUserLinkedParty.SendMasterUserLinkedParty( _httpClient, _COM_connectionString);

            MasterConsumableParty masterConsumableParty = new MasterConsumableParty(_darielURL);
            masterConsumableParty.SendMasterConsumableParty( _httpClient, _DTS_connectionString);
        }
    }
}
