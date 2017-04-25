using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Sql;
using Data = StoreClouding.SandBoxEngine.Terrain.Data;

namespace StoreClouding.SandBoxEngine.DAO.Terrain
{
    internal class BlockData
    {
        public static bool Insert(Data.BlockData data)
        {
            if (data == null || data.DataBaseID > -1)
                return false;

            var chunk = GameApplication.Current.Terrain.ChunkByMemoryID(data.ChunkMemoryID);
            if(chunk == null || chunk.DataBaseID <= -1)
                return false;

            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {
                SqlCommand comm = new SqlCommand(@"INSERT INTO dbo.BlockData(ChunkID,X,Y,Z,BlockID,IsoValue,IsPathBlocked,IsDestroyed) OUTPUT INSERTED.ID
                VALUES(@ChunkID, @X,@Y,@Z,@BlockID,@IsoValue,@IsPathBlocked,@IsDestroyed)", conn);
                comm.Parameters.AddWithValue("ChunkID", chunk.DataBaseID);
                comm.Parameters.AddWithValue("Y", data.Position.y);
                comm.Parameters.AddWithValue("X", data.Position.x);
                comm.Parameters.AddWithValue("Z", data.Position.z);
                comm.Parameters.AddWithValue("BlockID", data.Block.ID);
                comm.Parameters.AddWithValue("IsoValue", data.Isovalue);
                comm.Parameters.AddWithValue("IsPathBlocked", data.IsPathBlocked);
                comm.Parameters.AddWithValue("IsDestroyed", data.IsDestroyed);

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

        public static bool Update(Data.BlockData data)
        {
            if (data == null || data.DataBaseID <= -1)
                return false;

            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            try
            {
                SqlCommand comm = new SqlCommand(@"UPDATE dbo.BlockData
                                                   SET Y = @Y,
                                                       X = @X,
                                                       Z = @Z,
                                                       BlockID = @BlockID,
                                                       IsoValue = @IsoValue,
                                                       IsPathBlocked = @IsPathBlocked,
                                                       IsDestroyed = @IsDestroyed
                                                    WHERE ID = @ID", conn);
                comm.Parameters.AddWithValue("ID", data.DataBaseID);
                comm.Parameters.AddWithValue("Y", data.Position.y);
                comm.Parameters.AddWithValue("X", data.Position.x);
                comm.Parameters.AddWithValue("Z", data.Position.z);
                comm.Parameters.AddWithValue("BlockID", data.Block.ID);
                comm.Parameters.AddWithValue("IsoValue", data.Isovalue);
                comm.Parameters.AddWithValue("IsPathBlocked", data.IsPathBlocked);
                comm.Parameters.AddWithValue("IsDestroyed", data.IsDestroyed);

                int affected = comm.ExecuteNonQuery();

                return (affected > 0);
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
