using Aquazania.Telephony.Integration.Models;
using HTTPServer.Factory.MasterPartyContract.Impl;
using HTTPServer.Factory.MasterPartyContract;
using HTTPServer.Factory.MasterLinkedPartyContract.Impl;
using Aquazania.Integration.ServerApp.Factory.MasterLinkedPartyContract.Impl;

namespace HTTPServer.Factory.MasterLinkedPartyContract
{
    public class LinkedPartyFactory
    {
        enum PartyTypes { Contract, Customer, DeliveryAddress, Supplier, User, Contact }

        public static ILinkedPartyConvertor Create(ChangedLinkedContactContract party)
        {
            if (!Enum.TryParse(party.ParentPartyType, out PartyTypes partyType))
            {
                throw new NotSupportedException($"Party type {party.ParentPartyType} is not supported.");
            }

            switch (partyType)
            {
                case PartyTypes.Customer:
                    return new LinkedCustomerParty();
                case PartyTypes.Contact:
                    return new LinkedContactParty();
                case PartyTypes.Contract:
                    return new LinkedContractParty();
                case PartyTypes.DeliveryAddress:
                    return new LinkedDeliveryAddressParty();
                case PartyTypes.Supplier:  
                    return new LinkedSupplierParty();
                case PartyTypes.User: 
                    return new LinkedUserParty();
                default:
                    throw new NotSupportedException($"Party type {partyType} is not supported.");
            }
        }
    }
}
