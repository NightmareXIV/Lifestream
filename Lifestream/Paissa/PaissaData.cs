using Newtonsoft.Json;

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

    public enum PaissaStatus
    {
        Idle, Progress, Success, Error
    }
}