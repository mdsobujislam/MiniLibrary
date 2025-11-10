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
    public class BooksRepository : IBooksRepository
    {
        private readonly string _connectionString;

        public BooksRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(_connectionString));
        }
        public async Task<int> AddBookAsync(Book book)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var sql = @"INSERT INTO Books (Title,Author,ISBN,Category,CopiesAvailable,PublishedYear,Status,IsDeleted)
VALUES (@Title,@Author,@ISBN,@Category,@CopiesAvailable,@PublishedYear,@Status,0); SELECT CAST(SCOPE_IDENTITY() as int);";
                    return await connection.QuerySingleAsync<int>(sql, book);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error adding book", ex);
            }
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    return await connection.QueryFirstOrDefaultAsync<Book>("SELECT * FROM Books WHERE BookId = @Id AND IsDeleted = 0", new { Id = id });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving book by ID", ex);
            }
        }

        public async Task<(IEnumerable<Book> Items, int Total)> GetBooksAsync(string? title, string? category, string? isbn)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var where = "WHERE IsDeleted = 0";
                    var parameters = new DynamicParameters();
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        where += " AND Title LIKE @Title"; parameters.Add("Title", $"%{title}%");
                    }
                    if (!string.IsNullOrWhiteSpace(category)) { where += " AND Category LIKE @Category"; parameters.Add("Category", $"%{category}%"); }
                    if (!string.IsNullOrWhiteSpace(isbn)) { where += " AND ISBN LIKE @ISBN"; parameters.Add("ISBN", $"%{isbn}%"); }

                    var countSql = $"SELECT COUNT(*) FROM Books {where};";
                    var sql = $@" {countSql} SELECT * FROM Books {where} ORDER BY Title OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                    using var multi = await connection.QueryMultipleAsync(sql, parameters);
                    var total = await multi.ReadSingleAsync<int>();
                    var items = await multi.ReadAsync<Book>();
                    return (items, total);

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving books", ex);
            }
        }

        public async Task SoftDeleteBookAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    await connection.ExecuteAsync("UPDATE Books SET IsDeleted=1 WHERE BookId=@Id", new { Id = id });

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error soft deleting book", ex);
            }
        }

        public async Task UpdateBookAsync(Book book)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var sql = @"UPDATE Books SET Title=@Title,Author=@Author,ISBN=@ISBN,Category=@Category,CopiesAvailable=@CopiesAvailable,PublishedYear=@PublishedYear,Status=@Status WHERE BookId=@BookId";
                    await connection.ExecuteAsync(sql, book);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating book", ex);
            }
        }
    }
}
