using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.DTOs
{
    public class BorrowRequestDto
    {
        public int MemberId { get; set; }
        public DateTime DueDate { get; set; }
        public List<int> BookIds { get; set; } = new();
    }

}
