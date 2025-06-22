using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.AtkReaders;

namespace Lifestream.Schedulers;

internal static unsafe class WorldChange
{
    internal static bool? TargetValidAetheryte()
    {
        if(!Player.Available) return false;
        if(IsOccupied()) return false;
        var a = Utils.GetValidAetheryte();
        if(a != null)
        {
            if(a.Address != Svc.Targets.Target?.Address)
            {
                if(EzThrottler.Throttle("TargetValidAetheryte", 500))
                {
                    Svc.Targets.SetTarget(a);
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    internal static bool? InteractWithTargetedAetheryte()
    {
        if(!Player.Available) return false;
        if(Player.IsAnimationLocked) return false;
        if(!Utils.DismountIfNeeded()) return false;
        if(IsOccupied()) return false;
        var a = Utils.GetValidAetheryte();
        if(a != null && Svc.Targets.Target?.Address == a.Address)
        {
            if(EzThrottler.Throttle("InteractWithTargetedAetheryte", 500))
            {
                TargetSystem.Instance()->InteractWithObject(a.Struct(), false);
                return true;
            }
        }
        return false;
    }

    internal static bool? SelectAethernet()
    {
        if(!Player.Available) return false;
        return Utils.TrySelectSpecificEntry(Lang.Aethernet, () => EzThrottler.Throttle("SelectString"));
    }

    internal static bool? SelectVisitAnotherWorld()
    {
        if(!Player.Available) return false;
        return Utils.TrySelectSpecificEntry(Lang.VisitAnotherWorld, () => EzThrottler.Throttle("SelectString"));
    }

    internal static bool? ConfirmWorldVisit(string s)
    {
        if(!Player.Available) return false;
        var x = (AddonSelectYesno*)Utils.GetSpecificYesno(true, Lang.ConfirmWorldVisit);
        if(x != null)
        {
            if(x->YesButton->IsEnabled && EzThrottler.Throttle("ConfirmWorldVisit"))
            {
                new AddonMaster.SelectYesno(x).Yes();
                return true;
            }
        }
        return false;
    }

    internal static bool? SelectWorldToVisit(string world)
    {
        if(!Player.Available) return false;
        var worlds = Utils.GetAvailableWorldDestinations();
        var index = Array.IndexOf(worlds, world);
        if(index != -1)
        {
            if(TryGetAddonByName<AtkUnitBase>("WorldTravelSelect", out var addon) && IsAddonReady(addon))
            {
                if(EzThrottler.Throttle("SelectWorldToVisit", 1000))
                {
                    Callback.Fire(addon, true, index + 2);
                    return true;
                }
            }
        }
        return false;
    }

    /*internal static bool? TeleportToAethernetDestination(TinyAetheryte t)
    {
        if (!Player.Available) return false;
        if (TryGetAddonByName<AtkUnitBase>("TelepotTown", out var telep) && IsAddonReady(telep))
        {
            if (S.Data.DataStore.StaticData.Callback.TryGetValue(t.ID, out var callback))
            {
                if (Utils.GetAvailableAethernetDestinations().Any(x => x.Equals(t.Name)))
                {
                    if (EzThrottler.Throttle("TeleportToAethernetDestination", 2000))
                    {
                        P.TaskManager.InsertMulti(
                            new(() => Callback.Fire(telep, true, 11, callback)),
                            new(() => Callback.Fire(telep, true, 11, callback))
                            );
                        return true;
                    }
                }
                else
                {
                    PluginLog.Debug($"Could not find destination {t.Name}, attempting partial search...");
                    foreach (var destText in Utils.GetAvailableAethernetDestinations())
                    {
                        if (destText.Length > 20)
                        {
                            var text = destText[..^3];
                            if (t.Name.StartsWith(text))
                            {
                                if (EzThrottler.Throttle("TeleportToAethernetDestination", 2000))
                                {
                                    PluginLog.Debug($"Destination {t.Name} starts with {text}, assuming successful search");
                                    P.TaskManager.InsertMulti(
                                        new(() => Callback.Fire(telep, true, 11, callback)),
                                        new(() => Callback.Fire(telep, true, 11, callback))
                                        );
                                    return true;
                                }
                            }
                        }
                    }
                    if (EzThrottler.Throttle("TeleportToAethernetDestinationLog", 5000))
                    {
                        PluginLog.Warning($"GetAvailableAethernetDestinations does not contains {t.Name}, contains {Utils.GetAvailableAethernetDestinations().Print()}");
                    }
                }
            }
            else
            {
                DuoLog.Error($"Callback data absent for {t.Name}");
                return null;
            }
        }
        return false;
    }*/

    internal static bool? TeleportToAethernetDestination(string name)
    {
        if(!Player.Available) return false;
        if(TryGetAddonByName<AtkUnitBase>("TelepotTown", out var telep) && IsAddonReady(telep))
        {
            var reader = new ReaderTelepotTown(telep);
            for(var i = 0; i < reader.DestinationName.Count; i++)
            {
                if(reader.DestinationName[i].Name == name)
                {
                    var data = reader.DestinationData.SafeSelect(i);
                    if(data != null)
                    {
                        if(EzThrottler.Throttle("TeleportToAethernetDestination", 2000))
                        {
                            var callback = data.CallbackData;
                            P.TaskManager.InsertMulti(
                                new(() => Callback.Fire(telep, true, 11, callback)),
                                new(() => Callback.Fire(telep, true, 11, callback))
                                );
                            return true;
                        }
                    }
                }
            }
        }
        else if(S.Data.CustomAethernet.QuasiAethernetZones.Contains(P.Territory) && TryGetAddonMaster<AddonMaster.SelectString>(out var m) && m.IsAddonReady)
        {
            if(Utils.TryFindEqualsOrContains(m.Entries, e => e.Text, name, out var entry))
            {
                if(EzThrottler.Throttle("TeleportToAethernetDestination", 2000))
                {
                    entry.Select();
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool? ExecuteTPToAethernetDestination(uint destination, uint subIndex = 0)
    {
        if(!Player.Available) return false;
        if(AgentMap.Instance()->IsPlayerMoving == false && !IsOccupied() && !Player.Object.IsCasting && EzThrottler.Throttle("ExecTP", 1000))
        {
            return S.TeleportService.TeleportToAetheryte(destination, subIndex);
            //return Svc.PluginInterface.GetIpcSubscriber<uint, byte, bool>("Teleport").InvokeFunc(destination, (byte)subIndex);
        }
        return false;
    }

    internal static bool? WaitUntilNotBusy()
    {
        if(!Player.Available) return false;
        return S.Data.DataStore.Territories.Contains(P.Territory) && Player.Object.CastActionId == 0 && !IsOccupied() && !Utils.IsDisallowedToUseAethernet() && Player.Object.IsTargetable;
    }


    internal static bool? TargetReachableWorldChangeAetheryte()
    {
        return TargetReachableAetheryte(Utils.GetReachableWorldChangeAetheryte);
    }

    internal static bool? TargetReachableMasterAetheryte()
    {
        return TargetReachableAetheryte(Utils.GetReachableMasterAetheryte);
    }

    internal static bool? TargetReachableAetheryte(Func<bool, IGameObject> aetheryteFunc)
    {
        if(!Player.Available) return false;
        var a = aetheryteFunc(false);
        if(a.IsTarget()) return true;
        if(a != null)
        {
            if(!a.IsTarget() && EzThrottler.Throttle("TargetReachableAetheryte", 200))
            {
                Svc.Targets.SetTarget(a);
                return true;
            }
        }
        return false;
    }

    internal static bool? LockOn()
    {
        if(!Player.Available) return false;
        if(Svc.Targets.Target != null && EzThrottler.Throttle("LockOn", 200))
        {
            Chat.SendMessage("/lockon");
            return true;
        }
        return false;
    }

    internal static bool? EnableAutomove()
    {
        if(!Player.Available) return false;
        if(EzThrottler.Throttle("EnableAutomove", 200))
        {
            Chat.SendMessage("/automove on");
            return true;
        }
        return false;
    }

    internal static bool? WaitUntilMasterAetheryteExists()
    {
        if(!Player.Available) return false;
        return P.ActiveAetheryte != null && P.ActiveAetheryte.Value.IsAetheryte;
    }

    internal static bool? DisableAutomove()
    {
        if(!Player.Available) return false;
        if(EzThrottler.Throttle("DisableAutomove", 200))
        {
            Chat.SendMessage("/automove off");
            return true;
        }
        return false;
    }

    internal static bool? LeaveParty()
    {
        if(!Player.Available) return false;
        if(Svc.Party.Length < 2) return true;
        if(EzThrottler.Throttle("LeaveParty", 200))
        {
            Chat.SendMessage("/leave");
            return true;
        }
        return false;
    }

    internal static bool ClosePF()
    {
        if(TryGetAddonMaster<AddonMaster.LookingForGroupDetail>(out var m))
        {
            if(m.IsAddonReady && Utils.GenericThrottle) Callback.Fire(m.Base, true, -1);
        }
        else
        {
            return true;
        }
        return false;
    }

    internal static bool OpenSelfPF()
    {
        if(Player.Available)
        {
            if(Utils.GenericThrottle)
            {
                S.Memory.OpenPartyFinderInfoDetour(AgentLookingForGroup.Instance(), Player.CID);
                return true;
            }
        }
        return false;
    }

    internal static bool EndPF()
    {
        if(TryGetAddonMaster<AddonMaster.LookingForGroupDetail>(out var m) && m.IsAddonReady)
        {
            if(Utils.GenericThrottle)
            {
                m.TellEnd();
                return true;
            }
        }
        return false;
    }

    internal static bool WaitUntilNotRecruiting()
    {
        return !Svc.Condition[ConditionFlag.RecruitingWorldOnly];
    }

    internal static bool? LeaveAnyParty()
    {
        if(!Player.Available) return false;
        if(Svc.Party.Length < 2 && !Svc.Condition[ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance]) return true;
        if(EzThrottler.Throttle("LeaveParty", 200))
        {
            Chat.SendMessage("/leave");
            return true;
        }
        return false;
    }

    internal static bool? ConfirmLeaveParty()
    {
        if(!Player.Available) return false;
        if(Svc.Party.Length < 2) return true;
        var x = (AddonSelectYesno*)Utils.GetSpecificYesno();
        if(x != null)
        {
            if(x->YesButton->IsEnabled && EzThrottler.Throttle("ConfirmLeaveParty"))
            {
                new SelectYesnoMaster(x).Yes();
                return true;
            }
        }
        return false;
    }
}
