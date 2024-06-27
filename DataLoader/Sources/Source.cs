using System;

namespace DataLoader.Sources
{
    public class Source
    {
        public long Id { get; set; }
        public string NodeId { get; set; }
        public string SourceGroupId { get; set; }
        public DateTime Date { get; set; }
        public object Value { get; set; }
        public string ExternalId { get; set; }
    }
}
