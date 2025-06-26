using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Text.RegularExpressions;

namespace Lifestream.GUI;

public unsafe class SearchHelperOverlay : Window
{
    private List<CommandSuggestion> Suggestions = [];
    private List<CommandSuggestion> FilteredSuggestions = [];
    private string CurrentInput = "";
    private string FilterText = "";
    private Dictionary<string, string> CommandDescriptions = [];
    private Vector2 WindowSize;

    public SearchHelperOverlay() : base("##LifestreamSearchHelper",
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoSavedSettings, true)
    {
        ParseCommandDescriptions();
        RefreshSuggestions();
        IsOpen = false;
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    private void ParseCommandDescriptions()
    {
        CommandDescriptions.Clear();

        var helpLines = Lang.Help.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach(var line in helpLines)
        {
            if(line.StartsWith("--") || string.IsNullOrWhiteSpace(line))
                continue;

            var match = Regex.Match(line.Trim(), @"/li\s+(\w+).*?→\s*(.+)");
            if(match.Success)
            {
                var command = match.Groups[1].Value;
                var description = match.Groups[2].Value.Trim();

                var aliasMatch = Regex.Match(description, @"alias:\s*/li\s+(.+)");
                if(aliasMatch.Success)
                {
                    var aliases = aliasMatch.Groups[1].Value.Split('|');
                    foreach(var alias in aliases)
                    {
                        var cleanAlias = alias.Trim();
                        if(!CommandDescriptions.ContainsKey(cleanAlias))
                        {
                            CommandDescriptions[cleanAlias] = description.Replace(aliasMatch.Groups[0].Value, "").Trim();
                        }
                    }
                }

                if(!CommandDescriptions.ContainsKey(command))
                {
                    CommandDescriptions[command] = description;
                }
            }

            var basicMatch = Regex.Match(line.Trim(), @"/li\s*→\s*(.+)");
            if(basicMatch.Success && !CommandDescriptions.ContainsKey(""))
            {
                CommandDescriptions[""] = basicMatch.Groups[1].Value.Trim();
            }
        }

        var additionalDescriptions = new Dictionary<string, string>
        {
            ["help"] = "Show command help",
            ["?"] = "Show command help",
            ["commands"] = "Show command help",
            ["stop"] = "Stop all tasks"
        };

        foreach(var kvp in additionalDescriptions)
        {
            if(!CommandDescriptions.ContainsKey(kvp.Key))
            {
                CommandDescriptions[kvp.Key] = kvp.Value;
            }
        }
    }

    private void RefreshSuggestions()
    {
        Suggestions.Clear();
        var addedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddUniqueSuggestion(string command, string type, string description)
        {
            if(!addedCommands.Contains(command))
            {
                addedCommands.Add(command);
                Suggestions.Add(new CommandSuggestion(command, type, description));
            }
        }

        foreach(var cmd in Utils.LifestreamNativeCommands)
        {
            var description = CommandDescriptions.TryGetValue(cmd, out var desc)
                ? desc
                : "Built-in command";
            AddUniqueSuggestion(cmd, "Built-in", description);
        }

        var specialCommands = new[] { "help", "?", "commands", "stop" };
        foreach(var cmd in specialCommands)
        {
            if(CommandDescriptions.TryGetValue(cmd, out var desc))
            {
                AddUniqueSuggestion(cmd, "System", desc);
            }
        }

        foreach(var alias in C.CustomAliases.Where(x => x.Enabled && !string.IsNullOrEmpty(x.Alias)))
        {
            var stepCount = alias.Commands?.Count ?? 0;
            var desc = stepCount > 0
                ? $"Custom sequence with {stepCount} step{(stepCount != 1 ? "s" : "")}"
                : "Custom alias";
            AddUniqueSuggestion(alias.Alias, "Custom Alias", desc);
        }

        foreach(var folder in C.AddressBookFolders)
        {
            foreach(var entry in folder.Entries.Where(x => x.AliasEnabled && !string.IsNullOrEmpty(x.Alias)))
            {
                AddUniqueSuggestion(entry.Alias, "Address Book", entry.GetAddressString());
            }
        }

        if(S.Data.DataStore?.Worlds != null)
        {
            foreach(var world in S.Data.DataStore.Worlds)
            {
                AddUniqueSuggestion(world, "World", $"Travel to {world}");
            }
        }

        if(S.Data.DataStore?.DCWorlds != null)
        {
            foreach(var world in S.Data.DataStore.DCWorlds)
            {
                AddUniqueSuggestion(world, "DC World", $"Travel to {world} (cross-DC)");
            }
        }
    }

    public void UpdateFilter(string input)
    {
        CurrentInput = input;

        if(input.StartsWith("/li ", StringComparison.OrdinalIgnoreCase))
        {
            FilterText = input.Substring(4);
        }
        else if(input.StartsWith("/li", StringComparison.OrdinalIgnoreCase))
        {
            FilterText = input.Substring(3);
        }
        else
        {
            FilterText = "";
        }

        RefreshSuggestions();

        FilteredSuggestions = Suggestions.Where(s =>
            string.IsNullOrEmpty(FilterText) ||
            s.Command.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Command.StartsWith(FilterText, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(s => s.Command.Length)
            .ThenBy(s => s.Command)
            .Take(12)
            .ToList();
    }

    public override void PreDraw()
    {
        if(C.AutoCompletionFixedWindow)
        {
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new(
                    C.AutoCompletionWindowRight ? ImGuiHelpers.MainViewport.Size.X - C.AutoCompletionWindowOffset.X - WindowSize.X : C.AutoCompletionWindowOffset.X,
                    C.AutoCompletionWindowBottom ? ImGuiHelpers.MainViewport.Size.Y - C.AutoCompletionWindowOffset.Y - WindowSize.Y : C.AutoCompletionWindowOffset.Y
                    ));
        }
        else
        {
            if(TryGetAddonByName<AtkUnitBase>("ChatLog", out var chatAddon))
            {
                var chatPos = new Vector2(chatAddon->X, chatAddon->Y);
                var suggestionsPos = new Vector2(chatPos.X, chatPos.Y - 220);
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(suggestionsPos);
            }
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 6);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.One * 3);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xE0000000);
        ImGui.PushStyleColor(ImGuiCol.Border, 0xFF404040);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor(2);
    }

    public override void Draw()
    {
        if(FilteredSuggestions.Count == 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF808080);
            ImGui.Text("No matching commands found");
            ImGui.PopStyleColor();
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFFFFF);
        ImGui.Text($"Lifestream Commands{(string.IsNullOrEmpty(FilterText) ? "" : $" matching '{FilterText}'")}:");
        ImGui.PopStyleColor();
        ImGui.Separator();

        for(var i = 0; i < FilteredSuggestions.Count; i++)
        {
            var suggestion = FilteredSuggestions[i];

            var displayText = $"/li {suggestion.Command}";
            var typeText = $"[{suggestion.Type}]";
            var totalText = $"{displayText} {typeText}";
            var textSize = ImGui.CalcTextSize(totalText);
            var available = ImGui.GetContentRegionAvail();
            var buttonSize = new Vector2(Math.Max(available.X, textSize.X + 20), textSize.Y + 4);

            if(ImGui.InvisibleButton($"##suggest{i}", buttonSize))
            {
                CompleteCommand(suggestion.Command);
            }

            if(ImGui.IsItemHovered())
            {
                var drawList = ImGui.GetWindowDrawList();
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                drawList.AddRectFilled(min, max, 0x40FFFFFF);
            }

            ImGui.SetCursorPos(ImGui.GetCursorPos() - new Vector2(0, buttonSize.Y));

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00DD00);
            ImGui.Text(displayText);
            ImGui.PopStyleColor();

            ImGui.SameLine();

            var typeColor = suggestion.Type switch
            {
                "Built-in" => 0xFF4080FF,
                "System" => 0xFF8040FF,
                "Custom Alias" => 0xFFFF6000,
                "Address Book" => 0xFFFF0080,
                "World" => 0xFFFFD000,
                "DC World" => 0xFFFF4080,
                _ => 0xFF808080
            };

            ImGui.PushStyleColor(ImGuiCol.Text, typeColor);
            ImGui.Text(typeText);
            ImGui.PopStyleColor();
        }

        ImGui.Separator();
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFF808080);
        ImGui.Text("Click to complete");
        ImGui.PopStyleColor();
        WindowSize = ImGui.GetWindowSize();
    }

    private void CompleteCommand(string command)
    {
        try
        {
            var component = GetActiveTextInput();
            if(component != null)
            {
                var fullCommand = string.IsNullOrEmpty(command) ? "/li" : $"/li {command}";
                component->SetText(fullCommand);

                var finalCommand = string.IsNullOrEmpty(command) ? fullCommand : $"{fullCommand} ";
                component->SetText(finalCommand);
            }
        }
        catch(Exception ex)
        {
            PluginLog.Error($"Error completing command: {ex}");
        }

        IsOpen = false;
    }

    private static nint WantedVtblPtr
    {
        get
        {
            if(field == 0)
            {
                field = Svc.SigScanner.GetStaticAddressFromSig("48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 8B 48 68", 4);
            }
            return field;
        }
    } = 0;

    private unsafe AtkComponentTextInput* GetActiveTextInput()
    {
        var mod = RaptureAtkModule.Instance();
        if(mod == null) return null;

        var basePtr = mod->TextInput.TargetTextInputEventInterface;
        if(basePtr == null) return null;

        var vtblPtr = *(nint*)basePtr;
        if(vtblPtr != WantedVtblPtr) return null;

        return (AtkComponentTextInput*)((AtkComponentInputBase*)basePtr - 1);
    }

    public override bool DrawConditions()
    {
        return IsOpen && FilteredSuggestions.Count > 0;
    }
    public record CommandSuggestion(string Command, string Type, string Description);
}
