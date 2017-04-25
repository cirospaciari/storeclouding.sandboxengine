using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StoreClouding.SandBoxEngine
{
    public interface IApplication
    {
        bool Start(out string error);

        bool Update(out string error);

        bool Stop(out string error);
    }
}
