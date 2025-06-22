using Lifestream.Data;
using Lifestream.Enums;
using Newtonsoft.Json;
using NightmareUI.OtterGuiWrapper.FileSystems.Generic;

namespace Lifestream.Paissa;

public static class PaissaData
{
    public class PaissaResponse
    {
        [JsonProperty("id")]
        public int Id;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("districts")]
        public List<PaissaDistrict> Districts;
        [JsonProperty("num_open_plots")]
        public int NumOpenPlots;
        [JsonProperty("oldest_plot_time")]
        public float OldestPlotTime;
    }

    public class PaissaDistrict
    {
        [JsonProperty("id")]
        public int Id;
        [JsonProperty("name")]
        public string Name;
        [JsonProperty("num_open_plots")]
        public int NumOpenPlots;
        [JsonProperty("oldest_plot_time")]
        public float OldestPlotTime;
        [JsonProperty("open_plots")]
        public List<PaissaPlot> OpenPlots;
    }

    public class PaissaPlot
    {
        [JsonProperty("world_id")]
        public int WorldId;
        [JsonProperty("district_id")]
        public int DistrictId;
        [JsonProperty("ward_number")]
        public int WardNumber;
        [JsonProperty("plot_number")]
        public int PlotNumber;
        [JsonProperty("size")]
        public int Size;
        [JsonProperty("price")]
        public int Price;
        [JsonProperty("last_updated_time")]
        public float LastUpdatedTime;
        [JsonProperty("first_seen_time")]
        public float FirstSeenTime;
        [JsonProperty("est_time_open_min")]
        public float ESTTimeOpenMin;
        [JsonProperty("est_time_open_max")]
        public float ESTTimeOpenMax;
        [JsonProperty("purchase_system")]
        public int PurchaseSystem;
        [JsonProperty("lotto_entries")]
        public int? LottoEntries;
        [JsonProperty("lotto_phase")]
        public int? LottoPhase;
        [JsonProperty("lotto_phase_until")]
        public float? LottoPhaseUntil;
    }

    public class PaissaResult
    {
        public PaissaStatus Status { get; set; }
        public string FolderText { get; set; }
    }

    public enum PaissaStatus
    {
        Idle, Progress, Success, Error
    }

    public class PaissaAddressBookEntry : AddressBookEntry
    {
        public int Size = 1;
        public int Bids = 1;
        public int AllowedTenants = 1;
    }

    public static PaissaAddressBookEntry ToPaissa(this AddressBookEntry entry)
    {
        var tuple = entry.AsTuple();
        return new PaissaAddressBookEntry
        {
            Name = tuple.Name,
            World = tuple.World,
            City = (ResidentialAetheryteKind)tuple.City,
            Ward = tuple.Ward,
            PropertyType = (PropertyType)tuple.PropertyType,
            Plot = tuple.Plot,
            Apartment = tuple.Apartment,
            ApartmentSubdivision = tuple.ApartmentSubdivision,
            AliasEnabled = tuple.AliasEnabled,
            Alias = tuple.Alias,
            Size = 1,
            Bids = 1,
            AllowedTenants = 1,
        };
    }

    [Serializable]
    public class PaissaAddressBookFolder : IFileSystemStorage
    {
        internal bool IsCopy = false;
        public string ExportedName = "";
        public Guid GUID { get; set; } = Guid.NewGuid();
        public List<PaissaAddressBookEntry> Entries = [];
        public bool IsDefault = false;
        public SortMode SortMode = SortMode.Manual;

        public bool ShouldSerializeGUID() => !IsCopy;
        public bool ShouldSerializeIsDefault() => !IsCopy;
        public bool ShouldSerializeExportedName() => IsCopy;

        public string GetCustomName() => null;
        public void SetCustomName(string s) { }
    }
}