using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Domain.Entities
{
    public class BorrowItem
    {
        public int BorrowItemId { get; set; }
        public int BorrowId { get; set; }
        public int BookId { get; set; }
    }
}
