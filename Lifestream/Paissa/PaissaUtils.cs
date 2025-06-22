using ECommons.Configuration;
using ECommons.GameHelpers;
using static Lifestream.Paissa.PaissaData;

namespace Lifestream.Paissa;

public class PaissaUtils
{
    public static async Task<PaissaResult> ImportFromPaissaDBAsync()
    {
        try
        {
            var responseData = await GetListingsForHomeWorldAsync((int)Player.HomeWorldId);

            if(responseData.StartsWith("Error") || responseData.StartsWith("Exception"))
            {
                PluginLog.Error($"Error retrieving data: {responseData}");
                return new PaissaResult
                {
                    FolderText = "Error: Unable to retrieve listings. See log for details.",
                    Status = PaissaStatus.Error
                };
            }

            var responseObject = EzConfig.DefaultSerializationFactory.Deserialize<PaissaResponse>(responseData);
            if(responseObject == null)
            {
                PluginLog.Error("Failed to deserialize PaissaResponse.");
                return new PaissaResult
                {
                    FolderText = "Error: Invalid response format.",
                    Status = PaissaStatus.Error
                };
            }

            var newFolder = GetAddressBookFolderFromPaissaResponse(responseObject);

            new TickScheduler(() =>
            {
                if(Player.Available)
                {
                    PaissaImporter.Folders[Player.CurrentWorld] = newFolder;
                }
            });
            return new PaissaResult
            {
                FolderText = "Success!",
                Status = PaissaStatus.Success
            }; ;
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Exception in import task: {ex.Message}");
            return new PaissaResult
            {
                FolderText = $"Error: {ex.Message}",
                Status = PaissaStatus.Error
            };
        }
    }

    public static PaissaAddressBookFolder GetAddressBookFolderFromPaissaResponse(PaissaResponse paissaData)
    {
        List<PaissaAddressBookEntry> entries = [];

        foreach(var district in paissaData.Districts)
        {
            foreach(var plot in district.OpenPlots)
            {
                if(plot.LottoPhase != 1) continue;

                // Increment numbers by 1 because PaissaDB has them 0-indexed
                var wardStr = (plot.WardNumber + 1).ToString();
                var plotStr = (plot.PlotNumber + 1).ToString();
                var entry = BuildPaissaAddressBookEntry
                (
                    paissaData.Name,
                    district.Name,
                    wardStr,
                    plotStr,
                    false,
                    false,
                    $"{district.Name} Ward {wardStr} Plot {plotStr} ({GetCostString(plot.Price)})",
                    plot.Size,
                    plot.LottoEntries,
                    plot.PurchaseSystem
                );
                entries.Add(entry);
            }
        }

        PaissaAddressBookFolder folder = new()
        {
            ExportedName = "House Listings",
            Entries = entries,
            IsDefault = false,
            GUID = Guid.NewGuid()
        };

        return folder;
    }

    public static PaissaAddressBookEntry BuildPaissaAddressBookEntry(string worldStr, string cityStr, string wardNum, string plotApartmentNum, bool isApartment, bool isSubdivision, string name = null, int? size = null, int? bids = null, int? allowedTenants = null)
    {
        var baseEntry = Utils.BuildAddressBookEntry(worldStr, cityStr, wardNum, plotApartmentNum, isApartment, isSubdivision, name);
        var entry = baseEntry.ToPaissa();

        if(size != null) entry.Size = size.Value;
        if(bids != null) entry.Bids = bids.Value;
        if(allowedTenants != null) entry.AllowedTenants = allowedTenants.Value;

        return entry;
    }

    public static async Task<string> GetListingsForHomeWorldAsync(int worldId)
    {
        var url = $"https://paissadb.zhu.codes/worlds/{worldId}";

        var client = S.HttpClientProvider.Get();
        try
        {
            PluginLog.Debug($"Getting PaissaDB listings for World ID {worldId}...");
            var response = await client.GetAsync(url);

            if(response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                PluginLog.Debug("Response received successfully from PaissaDB:");
                PluginLog.Debug(responseData);
                return responseData;
            }
            else
            {
                var errorMessage = $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                PluginLog.Error(errorMessage);
                return errorMessage;
            }
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Exception occurred when getting house listings from PaissaDB: {ex.Message}");
            return $"Exception: {ex.Message}";
        }
    }

    public static string GetStatusStringFromStatus(PaissaStatus status)
    {
        return status switch
        {
            PaissaStatus.Idle => "",
            PaissaStatus.Progress => "Retrieving...",
            PaissaStatus.Success => "Success!",
            PaissaStatus.Error => "Error!",
            _ => "",
        };
    }

    public static Vector4 GetStatusColorFromStatus(PaissaStatus status)
    {
        return status switch
        {
            PaissaStatus.Idle => System.Drawing.KnownColor.White.Vector(),
            PaissaStatus.Progress => System.Drawing.KnownColor.White.Vector(),
            PaissaStatus.Success => System.Drawing.KnownColor.LimeGreen.Vector(),
            PaissaStatus.Error => System.Drawing.KnownColor.Red.Vector(),
            _ => System.Drawing.KnownColor.White.Vector()
        };
    }

    public static string GetSizeString(int size)
    {
        return size switch
        {
            0 => "Small",
            1 => "Medium",
            _ => "Large"
        };
    }

    private static string GetCostString(int cost)
    {
        return cost.ToString("N0") + "g";
    }

    public static string GetAllowedTenantsStringFromPurchaseSystem(int purchaseSystem)
    {
        return purchaseSystem switch
        {
            3 => "Free Company",
            5 => "Individual",
            7 => "Unrestricted",
            _ => "N/A"
        };
    }
}