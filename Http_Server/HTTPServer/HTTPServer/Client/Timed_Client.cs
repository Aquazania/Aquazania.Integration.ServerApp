using Aquazania.Integration.ServerApp.Client;
using Aquazania.Integration.ServerApp.Client.Consumable;
using Aquazania.Integration.ServerApp.Client.Contact;
using Aquazania.Integration.ServerApp.Client.Contract;
using Aquazania.Integration.ServerApp.Client.DeliveryAddress;
using Aquazania.Integration.ServerApp.Client.Supplier;
using Aquazania.Integration.ServerApp.Client.SupplierDeliveryAddress;
using Aquazania.Integration.ServerApp.Client.User;
using HTTPServer.Client.Customer;


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
        private async void CallBackFunctions(object state)
        {
            List<IMasterParty> masterParties = new List<IMasterParty>()
            {
                new MasterContactParty(_darielURL),
                new MasterContractParty(_darielURL),
                new MasterCustomerParty(_darielURL),
                new MasterDeliveryAddressParty(_darielURL),
                new MasterSupplierParty(_darielURL),
                new MasterSupplierDeliveryAddressParty(_darielURL),
                new MasterUserParty(_darielURL),
                new MasterConsumableParty(_darielURL)
            };

            List<IMasterLinkedParty> masterLinkedParties = new List<IMasterLinkedParty>()
            {
                new MasterContactLinkedParty(_darielURLContact),
                new MasterContractLinkedParty(_darielURLContact),
                new MasterCustomerLinkedParty(_darielURLContact),
                new MasterDeliveryAddressLinkedParty(_darielURLContact),
                new MasterSupplierLinkedParty(_darielURLContact),
                new MasterSupplierDeliveryAddressLinkedParty(_darielURLContact),
                new MasterUserLinkedParty(_darielURLContact)
            };

            foreach (IMasterParty party in masterParties)
            {
                await party.SendMasterParty(_httpClient, _DTS_connectionString);
            }

            foreach (IMasterLinkedParty linkedParty in masterLinkedParties)
            {
                await linkedParty.SendMasterLinkedParty(_httpClient, _COM_connectionString);
            }
        }
    }
}
