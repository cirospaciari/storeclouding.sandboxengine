using StoreClouding.SandBoxEngine.Client.Terrain.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Builder.Components
{
    public class Chunk : MonoBehaviour
    {
        public long ChunkMemoryID { get; set; }
        public MeshFilter Filter { get; private set; }
        public MeshCollider MeshCollider { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        public static Bounds ChunkBounds = new Bounds(
    new Vector3(Utils.Chunk.SIZE_X_TOTAL / 2, Utils.Chunk.SIZE_Y_TOTAL / 2, Utils.Chunk.SIZE_Z_TOTAL / 2),
    new Vector3(Utils.Chunk.SIZE_X_TOTAL, Utils.Chunk.SIZE_Y_TOTAL, Utils.Chunk.SIZE_Z_TOTAL));
        public static Chunk CreateComponent(long chunkMemoryID)
        {
            
            ChunkData chunkData = GameApplication.Current.Terrain.ChunkByMemoryID(chunkMemoryID);
            Map map = Map.Current;
            GameObject go = new GameObject("Chunk");
            var pos = chunkData.Position;
            go.transform.parent = map.transform;
            go.transform.localPosition = new Vector3(pos.x * Utils.Chunk.SIZE_X_TOTAL, pos.y * Utils.Chunk.SIZE_Y_TOTAL, pos.z * Utils.Chunk.SIZE_Z_TOTAL);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Chunk chunk = go.AddComponent<Chunk>();
            chunk.enabled = false;
            chunk.ChunkMemoryID = chunkMemoryID;
            //chunk.blockSet = map.GetBlockSet();
            //chunk.chunkData = chunkData;
            //chunk.threadManager = ThreadManager.GetInstance(map);
            /*if (map.generateGrass)
            {
                chunk.grassGenerator = GrassGenerator.CreateGrassGenerator(map, chunkData, chunk.transform);
                chunk.grassGenerator.PositionBase = chunk.transform.position;
            }*/
            chunk.MeshRenderer = go.AddComponent<MeshRenderer>();
            chunk.MeshRenderer.sharedMaterials = map.Materials;
            //go.GetComponent<Renderer>().castShadows = true;
            //TODO: VERIFICAR SE NÃO FIZ MERDA
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            go.GetComponent<Renderer>().receiveShadows = true;
            chunk.Filter = go.AddComponent<MeshFilter>();
            chunk.MeshCollider = go.AddComponent<MeshCollider>();
            chunk.MeshRenderer.enabled = false;
            chunk.MeshCollider.enabled = false;
#if TERRAVOL_RTP3
		chunkData.useRTP3 = true;
		go.AddComponent<ReliefTerrainVertexBlendTriplanar> ();
#endif

            return chunk;
        }


        /// <summary>
        /// Called once per frame. Checks if post build should be executed.</summary>
        public void Update()
        {
            ChunkData chunkData = GameApplication.Current.Terrain.ChunkByMemoryID(ChunkMemoryID);
            var built = GameApplication.Current.Terrain.Builder.PostBuild(chunkData);

            if (built)
                enabled = false;
        }
    }
}
