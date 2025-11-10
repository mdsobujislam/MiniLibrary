using Dapper;
using Microsoft.Extensions.Configuration;
using MiniLibrary.Application.Interfaces;
using MiniLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Infrastructure.Repositories
{
    public class MembersRepository : IMembersRepository
    {
        private readonly string _connectionString;

        public MembersRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(_connectionString));
        }
        public async Task<int> AddMemberAsync(Member member)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var sql = @"INSERT INTO Members (FullName,Email,Phone,JoinDate,IsActive,IsDeleted) VALUES (@FullName,@Email,@Phone,@JoinDate,@IsActive,0); SELECT CAST(SCOPE_IDENTITY() as int);";
                    return await connection.QuerySingleAsync<int>(sql, member);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding member", ex);
            }
        }

        public async Task<Member?> GetMemberByIdAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    return await connection.QueryFirstOrDefaultAsync<Member>("SELECT * FROM Members WHERE MemberId=@Id AND IsDeleted=0", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving member by ID", ex);
            }
        }

        public async Task<IEnumerable<Member>> GetMembersAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    return await connection.QueryAsync<Member>("SELECT * FROM Members WHERE IsDeleted = 0");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving members", ex);
            }
        }

        public async Task SoftDeleteMemberAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    await connection.ExecuteAsync("UPDATE Members SET IsDeleted=1 WHERE MemberId=@Id", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving members", ex);
            }
        }

        public async Task UpdateMemberAsync(Member member)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    await connection.ExecuteAsync("UPDATE Members SET FullName=@FullName,Email=@Email,Phone=@Phone,IsActive=@IsActive WHERE MemberId=@MemberId", member);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving members", ex);
            }
        }
    }
}
