using System;

namespace DataLoader.Repositories.Models
{
    internal class Period
    {
        public long Id { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
    }
}
