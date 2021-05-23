using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatCollarServer.Command
{
    public static class CommandFacade
    {
        private static Context context = new Context();

        public static void Process()
        {          
            bool isLast = false;
            while(!isLast)
            {
                ModelCommand.Add(ref context, "name");
                isLast = true;
            }
        }
    }
}
