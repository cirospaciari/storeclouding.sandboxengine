using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Map = StoreClouding.SandBoxEngine.Terrain.Map;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Collections.Concurrent;
using StoreClouding.SandBoxEngine.Terrain.Utils;

namespace StoreClouding.SandBoxEngine.DAO.Terrain
{
    internal static class Block
    {

        public static Map.BlockSet LoadBlockSet(out string error)
        {
            error = null;
            SqlConnection conn = GameApplication.Current.ConnectionManager.OpenConnection();
            var blockSet = new Map.BlockSet()
            {
                Blocks = new ConcurrentDictionary<int, Map.Block>()
            };
            try
            {
                
                SqlCommand comm = new SqlCommand("select * from Block", conn);

                
                using (var reader = comm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int ID = (int)reader["ID"];
                        int materialIndex = (int)reader["materialIndex"];
                        float colorA = Convert.ToSingle(reader["ColorA"]);
                        float colorR = Convert.ToSingle(reader["ColorR"]);
                        float colorG = Convert.ToSingle(reader["ColorG"]);
                        float colorB = Convert.ToSingle(reader["ColorB"]);

                        bool isDestructible = (bool)reader["IsDestructible"];
                        int priority = (int)reader["Priority"];
                        bool isVegetationEnabled = (bool)reader["IsVegetationEnabled"];
                        
                        blockSet.Blocks.GetOrAdd(ID, new Map.Block()
                        {
                            IsDestructible = isDestructible,
                            IsVegetationEnabled = isVegetationEnabled,
                            VertexColor = new Color(colorR, colorG, colorB, colorA),
                            MaterialIndex = materialIndex,
                            Priority = priority,
                            ID = ID
                        });

                    }

                }
                return blockSet;

            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return blockSet;
            }
            finally
            {
                GameApplication.Current.ConnectionManager.CloseConnection(conn);
            }
        }
    }
}
