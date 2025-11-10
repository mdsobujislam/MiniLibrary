using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.Interfaces
{
    public interface IBooksRepository
    {
        Task<(IEnumerable<Book> Items, int Total)> GetBooksAsync(string? title, string? category, string? isbn);
        Task<Book?> GetBookByIdAsync(int id);
        Task<int> AddBookAsync(Book book);
        Task UpdateBookAsync(Book book);
        Task SoftDeleteBookAsync(int id);
    }
}
