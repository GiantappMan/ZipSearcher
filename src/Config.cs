using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipSearcher
{
    public class Config
    {
        public List<ConfigItem> Configs { get; set; } = new List<ConfigItem>();
        public string LastConfig { get; set; }
    }

    public class ConfigItem
    {
        public string Name { get; set; }
        public string LastDir { get; set; }
        public string Spattern { get; set; }
    }
}
