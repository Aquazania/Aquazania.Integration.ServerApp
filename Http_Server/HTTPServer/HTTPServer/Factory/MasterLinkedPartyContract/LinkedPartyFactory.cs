using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract.Impl;
using HTTPServer.Factory.MasterPartyContract;
using HTTPServer.Factory.MasterLinkedPartyContract.Impl;

namespace HTTPServer.Factory.MasterLinkedPartyContract
{
    public class LinkedPartyFactory
    {
        enum PartyTypes { Contract, Customer, DeliveryAddress, Supplier, User, Contact }

        public static ILinkedPartyConvertor Create(ChangedLinkedContactContract party)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            if (!Enum.TryParse(party.ParentPartyType, out PartyTypes partyType))
            {
                throw new NotSupportedException($"Party type {party.ParentPartyType} is not supported.");
            }

            switch (partyType)
            {
                case PartyTypes.Customer:
                    return new LinkedCustomerParty(configuration);
                default:
                    throw new NotSupportedException($"Party type {partyType} is not supported.");
            }
        }
    }
}
