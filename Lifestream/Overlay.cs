using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    internal class Overlay : Window
    {
        public Overlay() : base("Lifestream Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize, true)
        {
            this.IsOpen = true;
            this.RespectCloseHotkey = false;
        }

        public override void Draw()
        {
            
        }
    }
}
