using Microsoft.Extensions.Configuration;
using MiniLibrary.Application.DTOs;
using MiniLibrary.Application.Interfaces;
using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Infrastructure.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly IBorrowRepository _repo;
        private readonly IConfiguration _cfg;
        public LibraryService(IBorrowRepository repo, IConfiguration cfg) { _repo = repo; _cfg = cfg; }

        public async Task<Borrow> BorrowBooksAsync(BorrowRequestDto dto)
        {
            // 2000ms delay
            await Task.Delay(2000);

            var active = await _repo.CountActiveBorrowsByMemberAsync(dto.MemberId);
            if (active + dto.BookIds.Count > 5) throw new InvalidOperationException("Member cannot have more than 5 active borrowed books.");

            var borrow = new Borrow
            {
                MemberId = dto.MemberId,
                BorrowDate = DateTime.UtcNow,
                DueDate = dto.DueDate,
                BorrowItems = dto.BookIds.Select(id => new BorrowItem { BookId = id }).ToList()
            };

            var id = await _repo.CreateBorrowAsync(borrow);
            borrow.BorrowId = id;
            return borrow;
        }

        public async Task ReturnBorrowAsync(int borrowId)
        {
            var perDay = _cfg.GetSection("Penalty").GetValue<decimal>("PerDay");
            await _repo.ReturnBorrowAsync(borrowId, perDay);
        }
    }

}
