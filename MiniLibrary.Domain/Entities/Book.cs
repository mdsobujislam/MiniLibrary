using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Domain.Entities
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int CopiesAvailable { get; set; }
        public int? PublishedYear { get; set; }
        public bool Status { get; set; }
        public bool IsDeleted { get; set; }
    }
}
