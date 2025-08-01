using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Data
{
    /// <summary>
    /// Represents metadata for a user interface entity, including its name, display name, description, and associated
    /// fields.
    /// </summary>
    /// <remarks>This class is typically used to define the structure and descriptive information for a UI
    /// entity,  such as a form or data model, in applications that dynamically generate user interfaces.</remarks>
    public class UIEntityMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<UIFieldMetadata> Fields { get; set; } = new List<UIFieldMetadata>();
    }
}
