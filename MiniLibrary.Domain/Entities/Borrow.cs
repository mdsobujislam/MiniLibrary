using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Domain.Entities
{
    public class Borrow
    {
        public int BorrowId { get; set; }
        public int MemberId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public decimal? PenaltyAmount { get; set; }
        public bool OverdueNotified { get; set; }
        public List<BorrowItem> BorrowItems { get; set; } = new();
    }
}
