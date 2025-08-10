using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleCommandSet.Models
{
    /// <summary>
    /// View information data structure
    /// </summary>
    public class ViewInfo
    {
        public long Id { get; set; }
        public string UniqueId { get; set; }
        public string Name { get; set; }
        public string ViewType { get; set; }
        public bool IsTemplate { get; set; }
        public int Scale { get; set; }
        public string DetailLevel { get; set; }
    }
}
