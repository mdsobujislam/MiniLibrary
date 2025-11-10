using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniLibrary.Application.Interfaces;

namespace MiniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IBorrowRepository _repo;
        private readonly IBooksRepository _repoBook;
        public ReportsController(IBorrowRepository repo, IBooksRepository repoBook) { _repo = repo;_repoBook = repoBook; }

        [HttpGet("borrow-summary")]
        public async Task<IActionResult> BorrowSummary([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var borrows = (await _repo.GetBorrowsByDateRangeAsync(from, to)).ToList();
            var totalBorrowed = borrows.Sum(b => b.BorrowItems.Count);
            var totalReturned = borrows.Count(b => b.ReturnDate != null);
            var active = borrows.Count(b => b.ReturnDate == null);
            var top = borrows.SelectMany(b => b.BorrowItems).GroupBy(i => i.BookId).Select(g => new { BookId = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).FirstOrDefault();
            string? topTitle = null;
            if (top != null)
            {
                var bk = await _repoBook.GetBookByIdAsync(top.BookId);
                topTitle = bk?.Title;
            }
            return Ok(new { TotalBooksBorrowed = totalBorrowed, TotalBooksReturned = totalReturned, ActiveBorrowRecords = active, MostBorrowedBookTitle = topTitle });
        }
    }

}
