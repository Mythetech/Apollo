namespace Apollo.Components.Solutions;

    public class Folder : ISolutionItem
    {
        public string Uri { get; set; }
        public string Name { get; set; }
        public List<ISolutionItem> Items { get; set; } = new List<ISolutionItem>();

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.Now;

        public void AddItem(ISolutionItem item)
        {
            Items.Add(item);
        }

        public ISolutionItem Clone()
        {
            return new Folder
            {
                Name = Name,
                Uri = Uri,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                Items = Items.Select(i => i.Clone()).ToList()
            };
        }
    }