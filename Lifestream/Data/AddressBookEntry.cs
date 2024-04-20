using Lifestream.Enums;

namespace Lifestream.Data;
[Serializable]
public class AddressBookEntry
{
    internal Guid GUID = Guid.NewGuid();
    public string Name = "";
    public int World = 21;
    public ResidentialAetheryte City = ResidentialAetheryte.Uldah;
    public int Ward = 1;
    public PropertyType PropertyType;
    public int Plot = 1;
    public int Apartment = 1;
    public bool ApartmentSubdivision = false;
    public bool AliasEnabled = false;
    public string Alias = "";
}
