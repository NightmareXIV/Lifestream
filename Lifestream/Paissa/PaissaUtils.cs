using Lifestream.Data;
using System.Net.Http;

namespace Lifestream.Paissa;

public class PaissaUtils
{
    public static AddressBookFolder GetAddressBookFolderFromPaissaResponse(PaissaResponse paissaData)
    {
        List<AddressBookEntry> entries = [];

        foreach (var district in paissaData.Districts)
        {
            foreach (var plot in district.OpenPlots)
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

        AddressBookFolder folder = new() {
            ExportedName = "House Listings",
            Entries = entries,
            IsDefault = false,
            GUID = Guid.NewGuid()
        };

        return folder;
    }

    public static async Task<string> GetListingsForHomeWorldAsync(int worldId)
    {
        string url = $"https://paissadb.zhu.codes/worlds/{worldId}";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                PluginLog.Debug($"Getting PaissaDB listings for World ID {worldId}...");
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    PluginLog.Debug("Response received successfully from PaissaDB:");
                    PluginLog.Debug(responseData);
                    return responseData;
                }
                else
                {
                    string errorMessage = $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                    PluginLog.Error(errorMessage);
                    return errorMessage;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception occurred when getting house listings from PaissaDB: {ex.Message}");
                return $"Exception: {ex.Message}";
            }
        }
    }

    private static string GetSizeString(int size)
    {
        if (size == 0) return "Small";
        else if (size == 1) return "Medium";
        else return "Large";
    }
}