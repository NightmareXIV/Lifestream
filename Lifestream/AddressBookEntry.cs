using Lifestream.Enums;

namespace Lifestream;
[Serializable]
public class AddressBookEntry
{
    public string Name = "";
    public int World = 21;
    public ResidentialAetheryte City = ResidentialAetheryte.Uldah;
    public int Ward = 1;
    public PropertyType PropertyType;
    public int Plot = 1;
    public int Apartment = 1;
}
