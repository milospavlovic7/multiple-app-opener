using System;

namespace WorkspaceLauncher.Models
{
    public class CapturedItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ActionType Type { get; set; }
        public string PathOrUrl { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
        public bool IsUrlValid { get; set; } = true; // True if URL extracted, False if we need manual input
        public string RawProcessName { get; set; } = string.Empty;
        public string RawWindowTitle { get; set; } = string.Empty;
    }
}
