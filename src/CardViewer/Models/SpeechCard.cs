namespace CardViewer.Models;

public class HierarchyItem
{
    public string Text { get; set; } = string.Empty;
    public int Indent { get; set; }
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = "Normal";
    public string Foreground { get; set; } = "#999999";
}

public class BulletItem
{
    public string Text { get; set; } = string.Empty;
    public int IndentLevel { get; set; }
    public int Indent => IndentLevel * 12;
}

public class SpeechCard
{
    public List<string> BreadcrumbPath { get; set; } = new();
    public string Topic { get; set; } = string.Empty;
    public string FullPath => string.Join(" > ", BreadcrumbPath.Append(Topic));
    public int CardNumber { get; set; }
    public int TotalCards { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<BulletItem> Bullets { get; set; } = new();
    public bool HasBullets => Bullets.Count > 0;

    /// <summary>
    /// Hierarchy items with escalating size — root is smallest/lightest,
    /// each level closer to the topic grows bolder and larger.
    /// </summary>
    public List<HierarchyItem> HierarchyItems
    {
        get
        {
            if (BreadcrumbPath.Count == 0)
                return new List<HierarchyItem>();

            var items = new List<HierarchyItem>();
            int count = BreadcrumbPath.Count;

            for (int i = 0; i < count; i++)
            {
                // Distance from the topic: count-1 is closest, 0 is farthest
                double ratio = count == 1 ? 1.0 : (double)i / (count - 1);

                // Font size scales from 9 (root) to 18 (closest parent)
                double size = 9 + ratio * 9;

                // Color fades from light (#BBBBBB) to darker (#555555)
                int gray = (int)(0xBB - ratio * 0x66);
                string color = $"#{gray:X2}{gray:X2}{gray:X2}";

                // Weight: light for distant, semibold for closest
                string weight = i == count - 1 ? "SemiBold" : "Normal";

                items.Add(new HierarchyItem
                {
                    Text = BreadcrumbPath[i],
                    Indent = i * 16,
                    FontSize = size,
                    FontWeight = weight,
                    Foreground = color
                });
            }

            return items;
        }
    }
}
