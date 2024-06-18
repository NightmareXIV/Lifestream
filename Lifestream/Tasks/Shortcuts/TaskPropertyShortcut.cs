using ECommons.GameHelpers;
using Lifestream.Schedulers;
using Lifestream.Tasks.CrossDC;
using Lifestream.Tasks.SameWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Tasks.Shortcuts;
public static class TaskPropertyShortcut
{
    public static void Enqueue(PropertyType propertyType = PropertyType.Auto)
    {
        if (P.TaskManager.IsBusy)
        {
            DuoLog.Error($"Lifestream is busy");
            return;
        }
        if (!Player.Available) return;
        if (!Player.IsInHomeWorld)
        {
            P.TPAndChangeWorld(Player.HomeWorld, !Player.IsInHomeDC, null, true, null, false, false);
        }
        P.TaskManager.Enqueue(() => Player.Interactable && Player.IsInHomeWorld && IsScreenReady());
        P.TaskManager.Enqueue(() =>
        {
            if (propertyType == PropertyType.Auto)
            {
                if (GetPrivateHouseAetheryteID() != 0)
                {
                    P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination(GetPrivateHouseAetheryteID()));
                }
                else if (GetFreeCompanyAetheryteID() != 0)
                {
                    P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination(GetFreeCompanyAetheryteID()));
                }
                else if (GetApartmentAetheryteID().ID != 0)
                {
                    EnqueueGoToMyApartment();
                }
                else
                {
                    DuoLog.Error($"Could not find private or free company house or apartment");
                }
            }
            else if (propertyType == PropertyType.Home)
            {
                if (GetPrivateHouseAetheryteID() != 0)
                {
                    P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination(GetPrivateHouseAetheryteID()));
                }
                else
                {
                    DuoLog.Error("Could not find private house");
                }
            }
            else if (propertyType == PropertyType.FC)
            {
                if (GetFreeCompanyAetheryteID() != 0)
                {
                    P.TaskManager.Insert(() => WorldChange.ExecuteTPToAethernetDestination(GetFreeCompanyAetheryteID()));
                }
                else
                {
                    DuoLog.Error("Could not find free company house");
                }
            }
            else if (propertyType == PropertyType.Apartment)
            {
                if (GetApartmentAetheryteID().ID != 0)
                {
                    EnqueueGoToMyApartment();
                }
                else
                {
                    DuoLog.Error("Could not find apartment");
                }
            }
        }, "ReturnToHomeTask");
    }

    private static void EnqueueGoToMyApartment()
    {
        var a = GetApartmentAetheryteID();
        P.TaskManager.BeginStack();
        P.TaskManager.Enqueue(() => WorldChange.ExecuteTPToAethernetDestination(a.ID, a.Sub));
        TaskApproachAndInteractWithApartmentEntrance.Enqueue();
        P.TaskManager.Enqueue(TaskApproachAndInteractWithApartmentEntrance.GoToMyApartment);
        P.TaskManager.InsertStack();
    }

    private static uint GetPrivateHouseAetheryteID()
    {
        foreach (var x in Svc.AetheryteList)
        {
            if (!x.IsAppartment && !x.IsSharedHouse && x.AetheryteId.EqualsAny<uint>(59, 60, 61, 97, 165))
            {
                return x.AetheryteId;
            }
        }
        return 0;
    }

    private static (uint ID, uint Sub) GetApartmentAetheryteID()
    {
        foreach (var x in Svc.AetheryteList)
        {
            if (x.IsAppartment && !x.IsSharedHouse && x.AetheryteId.EqualsAny<uint>(59, 60, 61, 97, 165))
            {
                return (x.AetheryteId, x.SubIndex);
            }
        }
        return (0, 0);
    }

    private static uint GetFreeCompanyAetheryteID()
    {
        foreach (var x in Svc.AetheryteList)
        {
            if (!x.IsAppartment && !x.IsSharedHouse && x.AetheryteId.EqualsAny<uint>(56, 57, 58, 96, 164))
            {
                return x.AetheryteId;
            }
        }
        return 0;
    }

    public enum PropertyType
    {
        Auto, Home, FC, Apartment
    }
}
