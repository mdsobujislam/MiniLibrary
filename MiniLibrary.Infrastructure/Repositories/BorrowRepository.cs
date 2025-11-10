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
    public class BorrowRepository : IBorrowRepository
    {
        private readonly string _connectionString;

        public BorrowRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(_connectionString));
        }
        public async Task<int> CountActiveBorrowsByMemberAsync(int memberId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    return await connection.QuerySingleAsync<int>("SELECT COUNT(*) FROM Borrows WHERE MemberId=@MemberId AND ReturnDate IS NULL", new { MemberId = memberId });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error counting active borrows by member", ex);
            }
        }

        public async Task<int> CreateBorrowAsync(Borrow borrow)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using var tran = connection.BeginTransaction();
                    try
                    {
                        var insertBorrow = @"INSERT INTO Borrows (MemberId,BorrowDate,DueDate,ReturnDate,PenaltyAmount,OverdueNotified) VALUES (@MemberId,@BorrowDate,@DueDate,NULL,NULL,0); SELECT CAST(SCOPE_IDENTITY() as int);";
                        var borrowId = await connection.QuerySingleAsync<int>(insertBorrow, new { borrow.MemberId, borrow.BorrowDate, borrow.DueDate }, tran);

                        foreach (var item in borrow.BorrowItems)
                        {
                            // check copies
                            var copies = await connection.QueryFirstOrDefaultAsync<int?>("SELECT CopiesAvailable FROM Books WHERE BookId=@BookId AND IsDeleted=0", new { BookId = item.BookId }, tran);
                            if (copies == null) throw new InvalidOperationException($"Book id {item.BookId} not found.");
                            if (copies <= 0) throw new InvalidOperationException($"No copies available for book id {item.BookId}.");

                            // update book
                            await connection.ExecuteAsync("UPDATE Books SET CopiesAvailable=CopiesAvailable-1, Status = CASE WHEN CopiesAvailable-1 <=0 THEN 0 ELSE 1 END WHERE BookId=@BookId", new { BookId = item.BookId }, tran);

                            // insert borrow item
                            await connection.ExecuteAsync("INSERT INTO BorrowItems (BorrowId,BookId) VALUES (@BorrowId,@BookId)", new { BorrowId = borrowId, BookId = item.BookId }, tran);
                        }

                        tran.Commit();
                        return borrowId;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error counting active borrows by member", ex);
            }
        }

        public async Task<Borrow?> GetBorrowByIdAsync(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var borrow = await connection.QueryFirstOrDefaultAsync<Borrow>("SELECT * FROM Borrows WHERE BorrowId=@Id", new { Id = id });
                    if (borrow == null) return null;
                    var items = await connection.QueryAsync<BorrowItem>("SELECT * FROM BorrowItems WHERE BorrowId=@BorrowId", new { BorrowId = id });
                    borrow.BorrowItems = items.ToList();
                    return borrow;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error counting active borrows by member", ex);
            }
        }

        public async Task<IEnumerable<Borrow>> GetBorrowsByDateRangeAsync(DateTime from, DateTime to)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var borrows = (await connection.QueryAsync<Borrow>("SELECT * FROM Borrows WHERE BorrowDate >= @From AND BorrowDate <= @To", new { From = from, To = to })).ToList();
                    if (!borrows.Any()) return borrows;
                    var ids = borrows.Select(b => b.BorrowId).ToArray();
                    var items = await connection.QueryAsync<BorrowItem>("SELECT * FROM BorrowItems WHERE BorrowId IN @Ids", new { Ids = ids });
                    var lookup = items.ToLookup(i => i.BorrowId);
                    foreach (var b in borrows) b.BorrowItems = lookup[b.BorrowId].ToList();
                    return borrows;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error counting active borrows by member", ex);
            }
        }

        public async Task ReturnBorrowAsync(int borrowId, decimal perDayPenalty)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using var tran = connection.BeginTransaction();
                    try
                    {
                        var borrow = await connection.QueryFirstOrDefaultAsync<Borrow>("SELECT * FROM Borrows WHERE BorrowId=@Id", new { Id = borrowId }, tran);
                        if (borrow == null) throw new InvalidOperationException("Borrow not found");
                        if (borrow.ReturnDate != null) throw new InvalidOperationException("Already returned");

                        var items = (await connection.QueryAsync<int>("SELECT BookId FROM BorrowItems WHERE BorrowId=@BorrowId", new { BorrowId = borrowId }, tran)).ToList();

                        // update return date
                        var returnDate = DateTime.UtcNow;
                        await connection.ExecuteAsync("UPDATE Borrows SET ReturnDate=@ReturnDate WHERE BorrowId=@BorrowId", new { ReturnDate = returnDate, BorrowId = borrowId }, tran);

                        // update book copies
                        foreach (var bookId in items)
                        {
                            await connection.ExecuteAsync("UPDATE Books SET CopiesAvailable = CopiesAvailable + 1, Status = CASE WHEN CopiesAvailable+1 > 0 THEN 1 ELSE 0 END WHERE BookId=@BookId", new { BookId = bookId }, tran);
                        }

                        // penalty calculation
                        if (returnDate > borrow.DueDate)
                        {
                            var daysLate = (int)Math.Ceiling((returnDate - borrow.DueDate).TotalDays);
                            var amount = (decimal)daysLate * perDayPenalty;
                            await connection.ExecuteAsync("INSERT INTO Penalties (BorrowId,DaysLate,Amount,CreatedAt) VALUES (@BorrowId,@DaysLate,@Amount,@CreatedAt)", new { BorrowId = borrowId, DaysLate = daysLate, Amount = amount, CreatedAt = DateTime.UtcNow }, tran);
                            await connection.ExecuteAsync("UPDATE Borrows SET PenaltyAmount=@Amount WHERE BorrowId=@BorrowId", new { Amount = amount, BorrowId = borrowId }, tran);
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error counting active borrows by member", ex);
            }
        }
    }
}
