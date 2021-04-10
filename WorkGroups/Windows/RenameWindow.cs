using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace The1nk.WorkGroups.Windows {
    public class RenameWindow : Dialog_Rename {
        public string SaveName = string.Empty;
        public RenameWindow(string name) {
            this.curName = name;
        }

        public Action<string> Action { get; set; }

        protected override AcceptanceReport NameIsValid(string name) {
            return (AcceptanceReport) true;
        }

        protected override void SetName(string name) {
            Action(curName);
        }
    }
}
