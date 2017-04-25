using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Data
{
    class BuildData
    {
        public Builder.Components.Chunk Component { get; set; }
        public long ChunkMemoryID { get; set; }
        public bool IsHarmonizable { get; set; }
        public bool BuildStarted { get; set; }
        public bool Built { get; set; }
        public MeshData TempMeshData { get; set; }
        public MeshData MeshData { get; set; }

        public BuildData(long ChunkMemoryID)
        {
            this.ChunkMemoryID = ChunkMemoryID;
            this.IsHarmonizable = false;
            this.BuildStarted = false;
            this.Built = false;
            this.TempMeshData = null;
            this.MeshData = null;
            this.Component = Builder.Components.Chunk.CreateComponent(ChunkMemoryID);
        }
    }
}
