using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniLibrary.Application.Interfaces;
using MiniLibrary.Domain.Entities;

namespace MiniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly IBooksRepository _repo;
        public BooksController(IBooksRepository repo) { _repo = repo; }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? title, [FromQuery] string? category, [FromQuery] string? isbn, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (items, total) = await _repo.GetBooksAsync(title, category, isbn, page, pageSize);
            return Ok(new { Items = items, Total = total, Page = page, PageSize = pageSize });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id) { var b = await _repo.GetBookByIdAsync(id); if (b == null) return NotFound(); return Ok(b); }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Book book) { book.Status = book.CopiesAvailable > 0; var id = await _repo.AddBookAsync(book); book.BookId = id; return CreatedAtAction(nameof(Get), new { id = id }, book); }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Book dto)
        {
            var exist = await _repo.GetBookByIdAsync(id);
            if (exist == null)
                return NotFound(new { Message = "Book not found" });

            dto.BookId = id;
            dto.Status = dto.CopiesAvailable > 0;

            await _repo.UpdateBookAsync(dto);

            return Ok(new { Message = "Book updated successfully" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) { await _repo.SoftDeleteBookAsync(id); return NoContent(); }
    }

}
