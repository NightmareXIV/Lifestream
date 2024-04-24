using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lifestream.Data;
[Serializable]
public class MultiPath
{
		internal Guid GUID = Guid.NewGuid();
		public string Name = "";
		public List<MultiPathEntry> Entries = [];
}
