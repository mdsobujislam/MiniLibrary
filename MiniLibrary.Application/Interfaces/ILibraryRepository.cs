using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.Interfaces
{
    public interface ILibraryRepository
    {
        // Books
        Task<(IEnumerable<Book> Items, int Total)> GetBooksAsync(string? title, string? category, string? isbn, int page, int pageSize);
        Task<Book?> GetBookByIdAsync(int id);
        Task<int> AddBookAsync(Book book);
        Task UpdateBookAsync(Book book);
        Task SoftDeleteBookAsync(int id);

        // Members
        Task<IEnumerable<Member>> GetMembersAsync();
        Task<Member?> GetMemberByIdAsync(int id);
        Task<int> AddMemberAsync(Member member);
        Task UpdateMemberAsync(Member member);
        Task SoftDeleteMemberAsync(int id);

        // Borrow
        Task<int> CreateBorrowAsync(Borrow borrow);
        Task<Borrow?> GetBorrowByIdAsync(int id);
        Task ReturnBorrowAsync(int borrowId, decimal perDayPenalty);
        Task<int> CountActiveBorrowsByMemberAsync(int memberId);
        Task<IEnumerable<Borrow>> GetBorrowsByDateRangeAsync(DateTime from, DateTime to);

        // Email log & Penalties retrieval if needed
    }
}
