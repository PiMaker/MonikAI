using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonikAI.Behaviours
{
    public interface IBehaviour
    {
        void Init(MainWindow window);
        void Update(MainWindow window);
    }
}
