using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace The1nk.WorkGroups.Models
{
    public static class UniqueId {
        private static int id = 8675309; // Jenny!

        public static int Next() {
            return id++;
        }
    }
}
