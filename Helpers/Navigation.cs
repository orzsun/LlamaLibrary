﻿using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Navigation;
using TreeSharp;

namespace LlamaLibrary.Helpers
{
    public static class Navigation
    {
        private static async Task<Queue<NavGraph.INode>> GenerateNodes(uint ZoneId, Vector3 xyz)
        {
            return await NavGraph.GetPathAsync((uint)ZoneId, xyz);

        }

        public static async Task<bool> GetTo(uint ZoneId, Vector3 XYZ)
        {
            var path = await GenerateNodes(ZoneId, XYZ );
            
            if (path == null)
                return false;
            
            if (path.Count < 1)
            {
                LogCritical($"Couldn't get a path to {XYZ} on {ZoneId}, Stopping.");
                return false;
            }
            
            object object_0 = new object();
            var composite =  NavGraph.NavGraphConsumer(j => path);

            while (path.Count > 0)
            {
                composite.Start(object_0);
                await Coroutine.Yield();
                while (composite.Tick(object_0) == RunStatus.Running)
                {
                    await Coroutine.Yield();
                }
                composite.Stop(object_0);
                await Coroutine.Yield();
            }
            
            Navigator.Stop();

            return Navigator.InPosition(Core.Me.Location, XYZ, 3);
        }
        
        public static void LogCritical(string text)
        {
            Logging.Write(Colors.OrangeRed, text);
        }
    }
}