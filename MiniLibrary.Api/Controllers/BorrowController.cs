using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniLibrary.Application.DTOs;
using MiniLibrary.Application.Interfaces;

namespace MiniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BorrowController : ControllerBase
    {
        private readonly ILibraryService _service;
        private readonly ILibraryRepository _repo;
        public BorrowController(ILibraryService service, ILibraryRepository repo) { _service = service; _repo = repo; }

        [HttpPost]
        public async Task<IActionResult> Borrow([FromBody] BorrowRequestDto dto)
        {
            try
            {
                var borrow = await _service.BorrowBooksAsync(dto);
                return Ok(borrow);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("return/{id}")]
        public async Task<IActionResult> Return(int id)
        {
            await _service.ReturnBorrowAsync(id);
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var b = await _repo.GetBorrowByIdAsync(id);
            if (b == null) return NotFound();
            return Ok(b);
        }
    }

}
