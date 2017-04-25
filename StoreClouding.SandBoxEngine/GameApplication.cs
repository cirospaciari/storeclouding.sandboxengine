using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Map = StoreClouding.SandBoxEngine.Terrain.Map;
using Data = StoreClouding.SandBoxEngine.Terrain.Data;
using DAO = StoreClouding.SandBoxEngine.DAO;
using Utils = StoreClouding.SandBoxEngine.Terrain.Utils;
using System.Collections.Concurrent;
using System.Threading;
using StoreClouding.SandBoxEngine.Terrain;
using StoreClouding.SandBoxEngine.Threading;
using StoreClouding.SandBoxEngine.Communication;
using StoreClouding.SandBoxEngine.DAO;
namespace StoreClouding.SandBoxEngine
{
    public class GameApplication : IApplication
    {
        internal const string ConnectionString = @"Data Source=CIRO-PC\SQLEXPRESS;Initial Catalog=SandBoxEngine;Integrated Security=True";

        public TerrainApplication Terrain { get; private set; }
        public ConnectionManagerApplication ConnectionManager { get; set; }
        public SocketControllerApplication SocketController { get; private set; }
        public static GameApplication Current { get; private set; }
        private List<ApplicationThread> updateThreads = new List<ApplicationThread>();

        static GameApplication()
        {
            Current = new GameApplication();
        }

        private GameApplication()
        {
            Terrain = new TerrainApplication();
            ConnectionManager = new ConnectionManagerApplication();
            SocketController = new SocketControllerApplication("127.0.0.1", 8080);
            //registra comunicação com o terreno
            foreach(var action in Terrain.SocketMessageTypes)
                SocketController.RegisterMessageType(action);

            var updateChunksProcess = new ApplicationThread(100);
            //update thread event
            updateChunksProcess.OnRun += (obj) =>
            {
                string error;
                if (!Terrain.UpdateChunks(out error))
                {
                    Log(error);
                }
            };
            updateThreads.Add(updateChunksProcess);


            var updateBlocksProcess = new ApplicationThread(100);
            //update thread event
            updateBlocksProcess.OnRun += (obj) =>
            {
                string error;
                if (!Terrain.UpdateBlocks(out error))
                {
                    Log(error);
                }
            };
            updateThreads.Add(updateBlocksProcess);

            var updateApplication = new ApplicationThread(100);
            //update thread event
            updateApplication.OnRun += (obj) =>
            {
                string error;
                if (!Terrain.UpdateBlocks(out error))
                {
                    Log(error);
                }
            };
            updateThreads.Add(updateApplication);
        }

        public bool Start(out string error)
        {
            if (!SocketController.Start(out error))
                return false;

            if (!ConnectionManager.Start(out error))
            {
                Log(error);
                return false;
            }

            if (!Terrain.Start(out error))
            {
                Log(error);
                return false;
            }
            //Run all
            foreach (var thread in updateThreads)
                thread.Start(thread);
            
            return true;
        }

        public bool Stop(out string error)
        {
            if (!SocketController.Stop(out error))
                return false;
            //stop all
            foreach (var thread in updateThreads)
                thread.Stop();

            //join all
            foreach (var thread in updateThreads)
                thread.Join();

            if (!Terrain.Stop(out error))
            {
                Log(error);
                return false;
            }

            if (!ConnectionManager.Stop(out error))
            {
                Log(error);
                return false;
            }
            return true;
        }

        public bool Update(out string error)
        {
            error = null;

            if (!SocketController.Update(out error))
                return false;

            if (!ConnectionManager.Update(out error))
                return false;

            /*if (!Terrain.Update(out error))
                return false;*/
            
            return true;
        }
        private void Log(string error)
        {
            Console.WriteLine(error);
        }
    }
}
