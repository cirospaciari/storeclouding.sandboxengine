using StoreClouding.SandBoxEngine.Client.Communication;
using StoreClouding.SandBoxEngine.Client.Terrain;
using StoreClouding.SandBoxEngine.Client.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client
{
   public class GameApplication : IApplication
    {
        public TerrainApplication Terrain { get; private set; }
        public SocketControllerApplication SocketController { get; private set; }
        
        public static GameApplication Current { get; private set; }
        private List<ApplicationThread> updateThreads = new List<ApplicationThread>();

        static GameApplication()
        {
            Current = new GameApplication();
        }
        public GameApplication()
        {
            Terrain = new TerrainApplication();
            SocketController = new SocketControllerApplication("127.0.0.1", 8080);
            
            foreach (var action in Terrain.SocketMessageTypes)
                SocketController.RegisterMessageType(action);
            
            var updateApplication = new ApplicationThread(100);
            //update thread event
            updateApplication.OnRun += (obj) =>
            {
                string updateError;
                if (!Update(out updateError))
                {
                    Log(updateError);
                }
            };
            updateThreads.Add(updateApplication);
        }

        public bool Start(out string error)
        {
            if (!SocketController.Start(out error))
                return false;

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

        public bool Update(out string error)
        {
            error = null;

            if (!SocketController.Update(out error))
                return false;

            /*if (!Terrain.Update(out error))
                return false;*/

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

            return Terrain.Stop(out error);
        }
        private void Log(string error)
        {
            Console.WriteLine(error);
        }
    }
}
