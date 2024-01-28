using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.GUI
{
    internal static class UIServiceAccount
    {
        internal static void Draw()
        {
            ImGuiEx.TextWrapped($"If you own more than 1 service accounts, you must assign each character to the correct service account.\nTo make character appear in this list, please log into it.");
            ImGui.Checkbox($"Get service account data from AutoRetainer", ref P.Config.UseAutoRetainerAccounts);
            List<string> ManagedByAR = [];
            if (P.AutoRetainerApi?.Ready == true && P.Config.UseAutoRetainerAccounts)
            {
                var chars = P.AutoRetainerApi.GetRegisteredCharacters();
                foreach (var c in chars)
                {
                    var data = P.AutoRetainerApi.GetOfflineCharacterData(c);
                    if (data != null)
                    {
                        var name = $"{data.Name}@{data.World}";
                        ManagedByAR.Add(name);
                        ImGui.SetNextItemWidth(150f);
                        if (ImGui.BeginCombo($"{name}", data.ServiceAccount == -1 ? "Not selected" : $"Service account {data.ServiceAccount+1}"))
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                if (ImGui.Selectable($"Service account {i + 1}"))
                                {
                                    P.Config.ServiceAccounts[name] = i;
                                    data.ServiceAccount = i;
                                    P.AutoRetainerApi.WriteOfflineCharacterData(data);
                                    Notify.Info($"Setting saved to AutoRetainer");
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        ImGuiEx.Text(ImGuiColors.DalamudRed, $"Managed by AutoRetainer");
                    }
                }
            }
            foreach (var x in P.Config.ServiceAccounts)
            {
                if (ManagedByAR.Contains(x.Key)) continue;
                ImGui.SetNextItemWidth(150f);
                if (ImGui.BeginCombo($"{x.Key}", x.Value==-1?"Not selected":$"Service account {x.Value+1}"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if(ImGui.Selectable($"Service account {i+1}")) P.Config.ServiceAccounts[x.Key] = i;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                if (ImGui.Button("Delete"))
                {
                    new TickScheduler(() => P.Config.ServiceAccounts.Remove(x.Key));
                }
            }
        }
    }
}
