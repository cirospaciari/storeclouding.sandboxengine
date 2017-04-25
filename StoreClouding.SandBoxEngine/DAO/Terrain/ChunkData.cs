using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Sql;
using Data = StoreClouding.SandBoxEngine.Terrain.Data;
using Utils = StoreClouding.SandBoxEngine.Terrain.Utils;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace StoreClouding.SandBoxEngine.DAO.Terrain
{
    internal static class ChunkData
    {
        public static long DataBaseCount(out string error)
        {
            error = null;
            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {

                SqlCommand comm = new SqlCommand("select COUNT_BIG(ID) as qty from dbo.Chunk", conn);

                using (var reader = comm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        long count = (long)reader["qty"];

                        return count;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return -1;
            }
            finally
            {
                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }

        private static bool LoadAllChunkBlocks(out string error)
        {
            error = null;
            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {

                SqlCommand comm = new SqlCommand("select * from dbo.BlockData", conn);
                //comm.Parameters.AddWithValue("ChunkID", chunkDataBaseID);
                comm.CommandTimeout = 10000;
                using (var reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        long ID = (long)reader["ID"];
                        long ChunkID = (long)reader["ChunkiD"];
                        byte X = (byte)reader["X"];
                        byte Y = (byte)reader["Y"];
                        byte Z = (byte)reader["Z"];
                        int BlockID = (int)reader["BlockID"];
                        float isovalue = Convert.ToSingle(reader["IsoValue"]);
                        bool IsDestroyed = (bool)reader["IsDestroyed"];
                        bool IsPathBlocked = (bool)reader["IsPathBlocked"];
                        var chunkData = GameApplication.Current.Terrain.ChunkByDataBaseID(ChunkID);
                        if (chunkData == null)
                        {
                            error = string.Format("Invalid ChunkData DataBaseID [DataBaseID:{0}]", ChunkID);
                            return false;
                        }
                        var blockData = new Data.BlockData(chunkData.MemoryID, ID, GameApplication.Current.Terrain.BlockSet.Blocks[BlockID], new SandBoxEngine.Terrain.Utils.Vector3i(X, Y, Z), isovalue, IsPathBlocked, IsDestroyed);
                        chunkData.Blocks[X][Y][Z] = blockData;

                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {

                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }

        private static bool LoadAllChunksWithNoBlocks(out string error)
        {
            error = null;
            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {
                SqlCommand comm = new SqlCommand("select * from dbo.Chunk", conn);
                comm.CommandTimeout = 10000;
                using (var reader = comm.ExecuteReader())
                {

                    //Nenhum Chunk
                    while (reader.Read())
                    {
                        long ID = (long)reader["ID"];
                        int x = (int)reader["X"];
                        int y = (int)reader["Y"];
                        int z = (int)reader["Z"];
                        int MapID = (int)reader["MapID"];
                        Data.ChunkData chunkData = new Data.ChunkData(MapID, ID, new SandBoxEngine.Terrain.Utils.Vector3i(x, y, z));
                        long MemoryID = GameApplication.Current.Terrain.AddChunk(chunkData);
                        if (MemoryID == -1)
                        {
                            error = string.Format("Failed to add chunk to memory [DataBaseID: {0}]", ID);
                            return false;
                        }
         
                    }

                    return true;

                }

            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }
            finally
            {
                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }

        public static bool LoadAllToApplication(out string error)
        {
            error = null;

            Console.WriteLine("Loading Chunks...");
            
            if (!LoadAllChunksWithNoBlocks(out error))
                return false;

            Console.WriteLine("Loading Blocks...");
            
            if (!LoadAllChunkBlocks(out error))
                return false;

            return true;

        }
        
        private static Data.ChunkData LoadChunkWithoutBlockData(Utils.Vector3i position, out string error)
        {
            error = null;
            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {
                SqlCommand comm = new SqlCommand("select * from dbo.Chunk where X = @X and Y = @Y and Z = @Z", conn);
                comm.Parameters.AddWithValue("X", position.x);
                comm.Parameters.AddWithValue("Y", position.y);
                comm.Parameters.AddWithValue("Z", position.z);
                using (var reader = comm.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }
                    long DataBaseID = (long)reader["ID"];
                    int x = (int)reader["X"];
                    int y = (int)reader["Y"];
                    int z = (int)reader["Z"];
                    int MapID = (int)reader["MapID"];


                    return new Data.ChunkData(MapID, DataBaseID, new SandBoxEngine.Terrain.Utils.Vector3i(x, y, z));
                    
                }

            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return null;
            }
            finally
            {
                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }

        public static Data.ChunkData Load(Utils.Vector3i position, out string error)
        {
            var chunk = LoadChunkWithoutBlockData(position, out error);
            if (!string.IsNullOrWhiteSpace(error) || chunk == null)
                return null;
            return Load(chunk.DataBaseID, out error);
        }
        public static Data.ChunkData Load(long DataBaseID, out string error)
        {
            error = null;
            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {
                if (DataBaseID <= -1)
                {
                    error = string.Format("Invalid chunk database id [DataBaseID: {0}]", DataBaseID);
                    return null;
                }
                StringBuilder query = new StringBuilder();
                query.AppendLine("select * from dbo.Chunk where ID = @chunkID");
                query.AppendLine(@"select * from dbo.BlockData where ChunkID = @chunkID");
                
                SqlCommand comm = new SqlCommand(query.ToString(), conn);
                comm.Parameters.AddWithValue("chunkID", DataBaseID);

                using (var reader = comm.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        error = string.Format("Invalid chunk not found [DataBaseID: {0}]", DataBaseID);
                        return null;
                    }
                    int x = (int)reader["X"];
                    int y = (int)reader["Y"];
                    int z = (int)reader["Z"];
                    int MapID = (int)reader["MapID"];


                    Data.ChunkData chunkData = new Data.ChunkData(MapID, DataBaseID, new SandBoxEngine.Terrain.Utils.Vector3i(x, y, z));
                    long MemoryID = GameApplication.Current.Terrain.AddChunk(chunkData);
                    lock (chunkData)
                    {
                        if (MemoryID == -1)
                        {
                            error = string.Format("Failed to add chunk to memory [DataBaseID: {0}]", DataBaseID);
                            return null;
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                long ID = (long)reader["ID"];
                                long ChunkID = (long)reader["ChunkiD"];
                                byte X = (byte)reader["X"];
                                byte Y = (byte)reader["Y"];
                                byte Z = (byte)reader["Z"];
                                int BlockID = (int)reader["BlockID"];
                                float isovalue = Convert.ToSingle(reader["IsoValue"]);
                                bool IsDestroyed = (bool)reader["IsDestroyed"];
                                bool IsPathBlocked = (bool)reader["IsPathBlocked"];

                                var blockData = new Data.BlockData(MemoryID, ID, GameApplication.Current.Terrain.BlockSet.Blocks[BlockID], new SandBoxEngine.Terrain.Utils.Vector3i(X, Y, Z), isovalue, IsPathBlocked, IsDestroyed);
                                chunkData.Blocks[X][Y][Z] = blockData;
                            }
                        }
                    }
                    return chunkData;
                }

            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return null;
            }
            finally
            {
                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }

        public static bool Insert(Data.ChunkData data)
        {
            if (data == null || data.DataBaseID > -1)
                return false;

            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {

                SqlCommand comm = new SqlCommand(@"INSERT INTO dbo.Chunk(MapID,X,Y,Z) OUTPUT INSERTED.ID
                VALUES(@MapID, @X,@Y,@Z)", conn);
                comm.Parameters.AddWithValue("MapID", data.MapID);
                comm.Parameters.AddWithValue("Y", data.Position.y);
                comm.Parameters.AddWithValue("X", data.Position.x);
                comm.Parameters.AddWithValue("Z", data.Position.z);

                long ID = (long)comm.ExecuteScalar();
                lock (data)
                {
                    data.DataBaseID = ID;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }
    }
}
