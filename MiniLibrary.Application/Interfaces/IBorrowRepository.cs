using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.Interfaces
{
    public interface IBorrowRepository
    {
        Task<int> CreateBorrowAsync(Borrow borrow);
        Task<Borrow?> GetBorrowByIdAsync(int id);
        Task ReturnBorrowAsync(int borrowId, decimal perDayPenalty);
        Task<int> CountActiveBorrowsByMemberAsync(int memberId);
        Task<IEnumerable<Borrow>> GetBorrowsByDateRangeAsync(DateTime from, DateTime to);
    }
}
