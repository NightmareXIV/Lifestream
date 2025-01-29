using Lifestream.Data;
using System.Net.Http;
using static Lifestream.Paissa.PaissaData;

namespace Lifestream.Paissa;

public class PaissaUtils
{
    public static AddressBookFolder GetAddressBookFolderFromPaissaResponse(PaissaResponse paissaData)
    {
        List<AddressBookEntry> entries = [];

        foreach(var district in paissaData.Districts)
        {
            foreach(var plot in district.OpenPlots)
            {
                // Increment numbers by 1 because PaissaDB has them 0-indexed
                var wardStr = (plot.WardNumber + 1).ToString();
                var plotStr = (plot.PlotNumber + 1).ToString();
                var entry = Utils.BuildAddressBookEntry
                (
                    paissaData.Name,
                    district.Name,
                    wardStr,
                    plotStr,
                    false,
                    false,
                    $"{district.Name} Ward {wardStr} Plot {plotStr} ({GetSizeString(plot.Size)})"
                );
                entries.Add(entry);
            }
        }

        AddressBookFolder folder = new()
        {
            ExportedName = "House Listings",
            Entries = entries,
            IsDefault = false,
            GUID = Guid.NewGuid()
        };

        return folder;
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

    private static string GetSizeString(int size)
    {
        return size switch
        {
            0 => "Small",
            1 => "Medium",
            _ => "Large"
        };
    }
}