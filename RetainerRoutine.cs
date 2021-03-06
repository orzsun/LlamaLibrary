﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using LlamaLibrary.Extensions;
using LlamaLibrary.RemoteWindows;
using LlamaLibrary.Retainers;
using static LlamaLibrary.Retainers.HelperFunctions;
using static ff14bot.RemoteWindows.Talk;

namespace LlamaLibrary
{
    public static class RetainerRoutine
    {
        public static string Name = "RetainerRoutine";
        
        internal async static Task<bool> ReadRetainers(Task retainerTask)
        {
            if (!RetainerList.Instance.IsOpen)
            {
                await UseSummoningBell();
                await Coroutine.Wait(5000, () => RetainerList.Instance.IsOpen);
            }

            if (!RetainerList.Instance.IsOpen)
            {
                LogCritical("Can't find open bell either you have none or not near a bell");
                return false;
            }

            var numRetainers = RetainerList.Instance.NumberOfRetainers;

            if (numRetainers <= 0)
            {
                LogCritical("Can't find number of retainers either you have none or not near a bell");
                RetainerList.Instance.Close();
                TreeRoot.Stop("Failed: Find a bell or some retainers");
                return true;
            }

            for (var retainerIndex = 0; retainerIndex < numRetainers; retainerIndex++)
            {
                Log($"Selecting {RetainerList.Instance.RetainerName(retainerIndex)}");
                await SelectRetainer(retainerIndex);
                
                await retainerTask;
                
                await DeSelectRetainer();
                Log($"Done with {RetainerList.Instance.RetainerName(retainerIndex)}");
            }

            RetainerList.Instance.Close();

            return true;
        }
        
        internal async static Task<bool> ReadRetainers(Func<Task> retainerTask)
        {
            if (!RetainerList.Instance.IsOpen)
            {
                await UseSummoningBell();
                await Coroutine.Wait(5000, () => RetainerList.Instance.IsOpen);
            }

            if (!RetainerList.Instance.IsOpen)
            {
                LogCritical("Can't find open bell either you have none or not near a bell");
                return false;
            }

            var numRetainers = RetainerList.Instance.NumberOfRetainers;

            if (numRetainers <= 0)
            {
                LogCritical("Can't find number of retainers either you have none or not near a bell");
                RetainerList.Instance.Close();
                TreeRoot.Stop("Failed: Find a bell or some retainers");
                return true;
            }

            for (var retainerIndex = 0; retainerIndex < numRetainers; retainerIndex++)
            {
                Log($"Selecting {RetainerList.Instance.RetainerName(retainerIndex)}");
                await SelectRetainer(retainerIndex);
                
                await retainerTask();
                
                await DeSelectRetainer();
                Log($"Done with {RetainerList.Instance.RetainerName(retainerIndex)}");
            }

            RetainerList.Instance.Close();

            return true;
        }
        
        internal async static Task<bool> ReadRetainers(Func<int,Task> retainerTask)
        {
            if (!RetainerList.Instance.IsOpen)
            {
                await UseSummoningBell();
                await Coroutine.Wait(5000, () => RetainerList.Instance.IsOpen);
            }

            if (!RetainerList.Instance.IsOpen)
            {
                LogCritical("Can't find open bell either you have none or not near a bell");
                return false;
            }

            var numRetainers = RetainerList.Instance.NumberOfRetainers;

            if (numRetainers <= 0)
            {
                LogCritical("Can't find number of retainers either you have none or not near a bell");
                RetainerList.Instance.Close();
                TreeRoot.Stop("Failed: Find a bell or some retainers");
                return true;
            }

            for (var retainerIndex = 0; retainerIndex < numRetainers; retainerIndex++)
            {
                Log($"Selecting {RetainerList.Instance.RetainerName(retainerIndex)}");
                await SelectRetainer(retainerIndex);
                
                await retainerTask(retainerIndex);
                
                await DeSelectRetainer();
                Log($"Done with {RetainerList.Instance.RetainerName(retainerIndex)}");
            }

            RetainerList.Instance.Close();

            return true;
        }
        
        internal static async Task DumpItems()
        {
                var playerItems = InventoryManager.GetBagsByInventoryBagId(PlayerInventoryBagIds).Select(i => i.FilledSlots).SelectMany(x => x).AsParallel()
                    .Where(FilterStackable);
                
                var retItems = InventoryManager.GetBagsByInventoryBagId(RetainerBagIds).Select(i => i.FilledSlots).SelectMany(x => x).AsParallel()
                    .Where(FilterStackable);

                var sameItems  = playerItems.Intersect(retItems, new BagSlotComparer());
                foreach (var slot in sameItems)
                {
                    LogLoud($"Want to move {slot}");
                    slot.RetainerEntrustQuantity((int) slot.Count);
                    await Coroutine.Sleep(100);
                }
        }
        

        
        
        internal static async Task<bool> SelectRetainer(int retainerIndex)
        {
            if (RetainerList.Instance.IsOpen) return await RetainerList.Instance.SelectRetainer(retainerIndex);

            if (RetainerTasks.IsOpen)
            {
                RetainerTasks.CloseTasks();
                await Coroutine.Wait(1500, () => DialogOpen);
                await Coroutine.Sleep(200);
                if (DialogOpen) Next();
                await Coroutine.Sleep(200);
                await Coroutine.Wait(3000, () => RetainerList.Instance.IsOpen);
                return await RetainerList.Instance.SelectRetainer(retainerIndex);
            }

            if (!RetainerList.Instance.IsOpen && NearestSummoningBell() != null)
            {
                await UseSummoningBell();
                await Coroutine.Wait(5000, () => RetainerList.Instance.IsOpen);
                return await RetainerList.Instance.SelectRetainer(retainerIndex);
            }

            return false;
        }

        internal static async Task<bool> DeSelectRetainer()
        {
            if (!RetainerTasks.IsOpen) return true;
            RetainerTasks.CloseTasks();
            
            await Coroutine.Wait(1500, () => DialogOpen || SelectYesno.IsOpen);
            if (SelectYesno.IsOpen)
            {
                SelectYesno.Yes();
                await Coroutine.Wait(1500, () => DialogOpen);
            }
            await Coroutine.Sleep(200);
            if (DialogOpen) Next();
            await Coroutine.Sleep(200);
            return await Coroutine.Wait(3000, () => RetainerList.Instance.IsOpen);
        }
        
        class BagSlotComparer : IEqualityComparer<BagSlot>
        {
            public bool Equals(BagSlot x, BagSlot y)
            {
                return x.RawItemId == y.RawItemId && (x.Count + y.Count < x.Item.StackSize);
            }

            public int GetHashCode(BagSlot obj)
            {
                return obj.Item.GetHashCode();
            }
        }
        
        private static void  Log(string text, params object[] args)
        {
            var msg = string.Format("[" + Name + "] " + text, args);
            Logging.Write(Colors.Green, msg);
        }

        public static void LogLoud(string text, params object[] args)
        {
            var msg = string.Format("[" + Name + "] " + text, args);
            Logging.Write(Colors.Goldenrod, msg);
        }
        
        private static void LogCritical(string text)
        {
            Logging.Write(Colors.OrangeRed, "[" + Name + "] " + text);
        }
    }
}