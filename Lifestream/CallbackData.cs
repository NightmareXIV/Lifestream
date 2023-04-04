using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream
{
    public class CallbackData : IEzConfig
    {
        public Dictionary<uint, uint> Data = new();
    }
}
