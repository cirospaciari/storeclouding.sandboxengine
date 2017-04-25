using StoreClouding.SandBoxEngine.Terrain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace StoreClouding.SandBoxEngine.Terrain.Map
{
    /// <summary>
    /// This defines a block type.</summary>
    [Serializable]
    public class Block
    {
        private int materialIndex;
        
        private Color vertexColor;
        
        private bool isDestructible;
        
        private int priority;
        
        private bool isVegetationEnabled;

        public int ID { get; set; }

        public int Priority
        {
            get
            {
                return priority;
            }
            set
            {
                priority = value;
            }
        }

        public int MaterialIndex
        {
            get
            {
                return materialIndex;
            }
            set
            {
                materialIndex = value;
            }
        }

        public Color VertexColor
        {
           
            get
            {
                
                return vertexColor;
            }
            set
            {
                vertexColor = value;
            }
        }

        public bool IsVegetationEnabled
        {
            get
            {
                return isVegetationEnabled;
            }
            set
            {
                isVegetationEnabled = value;
            }
        }

        public bool IsDestructible
        {
            get
            {
                return isDestructible;
            }
            set
            {
                isDestructible = value;
            }
        }

    }

}
