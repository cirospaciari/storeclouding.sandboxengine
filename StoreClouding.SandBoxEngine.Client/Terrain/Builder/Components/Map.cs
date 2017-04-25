using StoreClouding.SandBoxEngine.Client.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StoreClouding.SandBoxEngine.Client.Terrain.Builder.Components
{
    /// <summary>
    /// TerraMap component. This is the core component of TerraVol.</summary>
    /// <remarks>
    /// Requires TerraMapGenerator component.</remarks>
    [AddComponentMenu("TerraVol/Map")]
    class Map : MonoBehaviour
    {
        [SerializeField]
        public Camera PlayerCamera;

        [SerializeField]
        public Material[] Materials;
        public static Map Current {get; private set;}
        [SerializeField]
        // Grass
        [SerializeField]
        public bool generateGrass;
        [SerializeField]
        public Material grassMaterial;
        [SerializeField]
        public float grassSize = 0.4f;
        [SerializeField]
        public float grassHeight = 3.5f;
        [SerializeField]
        public float grassDirtyHeight = 1f;
        [SerializeField]
        public float grassMinHeight = 0.5f;
        [SerializeField]
        public float grassTextureTileX = 2f;
        [SerializeField]
        public int grassDensity = 2;
        [SerializeField]
        public Color grassColor = new Color(0.6f, 0.6f, 0.6f, 0);
        [SerializeField]
        public Color grassDirtyColor = new Color(0.722f, 0.573f, 0.102f, 0);
        [SerializeField]
        public float grassMaxSlopeAngle = 30f;
        [SerializeField]
        public float windStrength;
        [SerializeField]
        public float grassDrawDistance;
        private Vector4 grassWaving;
        private bool grassStandardShader = false;
        private bool grassWavingShader = false;
        private float grassDrawDistanceSquared;
        private Vector3i LastUpdatedPosition;
        public void Awake()
        {
            Current = this;
        }
        // Called once at start
        public void Start()
        {
            // Shaders
            if (grassMaterial != null)
            {
                grassDrawDistanceSquared = grassDrawDistance * grassDrawDistance;

                if (grassMaterial.HasProperty("_WaveAndDistance"))
                {
                    grassWaving = grassMaterial.GetVector("_WaveAndDistance");
                    grassWaving.w = grassDrawDistanceSquared;
                    grassMaterial.SetVector("_WaveAndDistance", grassWaving);
                }

                grassWavingShader = grassMaterial.HasProperty("_WaveAndDistance");
                grassStandardShader = grassMaterial.HasProperty("_CameraPosition");
            }

            // Prepare WorldRecorder
            //WorldRecorder.Instance.worldMinY = chunks.MinY;
            //WorldRecorder.Instance.worldMaxY = chunks.MaxY;

            // Start all threads
            //ThreadManager.GetInstance(this).StartAll();
        }

        public void Update()
        {
            // Update grass shader to handle wind animation
            UpdateGrassShader();
            Vector3 pos = PlayerCamera.transform.position;
            UpdateAround(pos, true);
        }

        /// <summary>
        /// Generate terrain around the position.</summary>
        /// <param name="pos"> Terrain will be generated around this position.</param>
        /// <param name="threaded"> If true, terrain generation & building will be multithreaded.</param>
        public void UpdateAround(Vector3 pos, bool threaded)
        {
            try
            {
                int posX = Mathf.RoundToInt(pos.x / Utils.Chunk.SIZE_X_BLOCK);
                int posY = Mathf.RoundToInt(pos.y / Utils.Chunk.SIZE_Y_BLOCK);
                int posZ = Mathf.RoundToInt(pos.z / Utils.Chunk.SIZE_Z_BLOCK);
                Vector3i current = Utils.Chunk.ToChunkPosition(posX, posY, posZ);
                if (current != LastUpdatedPosition)
                {
                    genDone = false;
                    buildDone = false;
                    grassDone = false;
                }

                // Hide chunks far away
                this.FreeColumns(current.x, current.z);

                // Generate dynamically
                /*if (!genDone)
                {
                    List<Vector3i> nearEmpties = LazyFindNearestNotGeneratedColumn(current.x, current.z, map.buildDistance + 1, 1, 1);
                    if (nearEmpties.Count != 0)
                    {
                        for (int i = 0; i < nearEmpties.Count; i++)
                        {
                            Vector3i nearEmpty = nearEmpties[i];
                            int cx = nearEmpty.x;
                            int cz = nearEmpty.z;
                            if (threaded)
                                GenerateColumnThreaded(GetChunk2D(cx, cz));
                            else
                                GenerateColumn(cx, cz);
                        }
                    }
                    else
                    {
                        genDone = true;
                    }
                }*/

                // Build dynamically
                if (!buildDone)
                {
                    List<Vector3i> nearEmpties = LazyFindNearestNotBuiltColumn(current.x, current.z, map.buildDistance, 1, 1);
                    if (nearEmpties.Count != 0)
                    {
                        for (int i = 0; i < nearEmpties.Count; i++)
                        {
                            Vector3i nearEmpty = nearEmpties[i];
                            int cx = nearEmpty.x;
                            int cz = nearEmpty.z;
                            if (NeighbourColumnsAreGenerated(cx, cz))
                            {
                                if (threaded)
                                    BuildColumnThreaded(GetChunk2D(cx, cz));
                                else
                                    BuildColumn(cx, cz);
                            }
                        }
                    }
                    else
                    {
                        buildDone = true;
                    }
                }



                // Display grass
                if (threaded && !grassDone)
                {
                    Vector3i? nearEmpty = LazyFindNearestNoGrassColumn(current.x, current.z, (int)(map.grassDrawDistance / (Chunk.SIZE_AVERAGE_BLOCK * Chunk.SIZE_X)) + 1, 1);
                    if (nearEmpty.HasValue)
                    {
                        int cx = nearEmpty.Value.x;
                        int cz = nearEmpty.Value.z;
                        BuildGrassColumn(cx, cz);
                    }
                    else
                    {
                        grassDone = true;
                    }
                }

                LastUpdatedPosition = current;
            }
            catch (Exception e)
            {
                Debug.LogWarning("TerraVol Editor Tool tried to execute terrain generation while it was null: " + e.Message);
            }
        }
        public void UpdateAll()
        {
            // Force all chunks to update
            foreach (Transform child in transform)
            {
                Chunk childChunk = child.GetComponent<Chunk>();
                if (childChunk != null && childChunk.enabled)
                {
                    childChunk.Update();
                }
            }
        }
        // Update grass shader to handle wind animation
        private void UpdateGrassShader()
        {
            // Update camera position
            if (grassStandardShader)
            {
                Vector3 cpos = Camera.main.transform.position;
                Vector4 _CameraPosition = new Vector4(cpos.x,
                                                cpos.y,
                                                cpos.z, 1f / grassDrawDistanceSquared);

                grassMaterial.SetVector("_CameraPosition", _CameraPosition);
            }
            // Update waving shader
            if (grassWavingShader)
            {
                grassWaving.x += 0.01f * windStrength * Time.deltaTime;
                grassMaterial.SetVector("_WaveAndDistance", grassWaving);
            }
        }
    }
}
