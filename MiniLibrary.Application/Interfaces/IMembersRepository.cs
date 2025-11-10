using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.Interfaces
{
    public interface IMembersRepository
    {
        Task<IEnumerable<Member>> GetMembersAsync();
        Task<Member?> GetMemberByIdAsync(int id);
        Task<int> AddMemberAsync(Member member);
        Task UpdateMemberAsync(Member member);
        Task SoftDeleteMemberAsync(int id);
    }
}
