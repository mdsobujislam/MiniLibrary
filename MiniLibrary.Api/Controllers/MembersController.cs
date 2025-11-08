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
    public class MembersController : ControllerBase
    {
        private readonly ILibraryRepository _repo;
        public MembersController(ILibraryRepository repo) { _repo = repo; }

        [HttpGet] public async Task<IActionResult> Get() => Ok(await _repo.GetMembersAsync());
        [HttpGet("{id}")] public async Task<IActionResult> Get(int id) { var m = await _repo.GetMemberByIdAsync(id); if (m == null) return NotFound(); return Ok(m); }
        [HttpPost] public async Task<IActionResult> Create([FromBody] Member member) { var id = await _repo.AddMemberAsync(member); member.MemberId = id; return CreatedAtAction(nameof(Get), new { id = id }, member); }
        [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] Member member) { var exist = await _repo.GetMemberByIdAsync(id); if (exist == null) return NotFound(); member.MemberId = id; await _repo.UpdateMemberAsync(member); return NoContent(); }
        [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) { await _repo.SoftDeleteMemberAsync(id); return NoContent(); }
    }

}
