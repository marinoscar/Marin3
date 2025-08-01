using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Data
{
    /// <summary>
    /// Provides metadata for fields in a data model that will be used to create UI forms.
    /// </summary>
    public class UIFieldMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsForeignKey { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public int MaxLength { get; set; } = 0;

    }
}
