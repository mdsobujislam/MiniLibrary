using MiniLibrary.Application.DTOs;
using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.Interfaces
{
    public interface ILibraryService
    {
        Task<Borrow> BorrowBooksAsync(BorrowRequestDto dto);
        Task ReturnBorrowAsync(int borrowId);
    }
}
