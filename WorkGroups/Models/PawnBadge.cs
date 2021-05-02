using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups.Models
{
    public struct PawnBadge {
        public Def Def;
        public Texture2D Texture;

        public PawnBadge(Def def, Texture2D texture) {
            this.Def = def;
            this.Texture = texture;
        }
    }
}
