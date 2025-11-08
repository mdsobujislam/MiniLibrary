using Dapper;
using MiniLibrary.Application.Interfaces;
using MiniLibrary.Domain.Entities;
using MiniLibrary.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Infrastructure.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly IDbConnectionFactory _db;
        public LibraryRepository(IDbConnectionFactory db) { _db = db; }

        // Books with pagination
        public async Task<(IEnumerable<Book> Items, int Total)> GetBooksAsync(string? title, string? category, string? isbn, int page, int pageSize)
        {
            using var conn = _db.CreateConnection();
            var where = "WHERE IsDeleted = 0";
            var parameters = new DynamicParameters();
            if (!string.IsNullOrWhiteSpace(title)) { where += " AND Title LIKE @Title"; parameters.Add("Title", $"%{title}%"); }
            if (!string.IsNullOrWhiteSpace(category)) { where += " AND Category LIKE @Category"; parameters.Add("Category", $"%{category}%"); }
            if (!string.IsNullOrWhiteSpace(isbn)) { where += " AND ISBN LIKE @ISBN"; parameters.Add("ISBN", $"%{isbn}%"); }

            var countSql = $"SELECT COUNT(*) FROM Books {where};";
            var sql = $@"
{countSql}
SELECT * FROM Books {where} ORDER BY Title OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            parameters.Add("Offset", (page - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            using var multi = await conn.QueryMultipleAsync(sql, parameters);
            var total = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<Book>();
            return (items, total);
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Book>("SELECT * FROM Books WHERE BookId = @Id AND IsDeleted = 0", new { Id = id });
        }

        public async Task<int> AddBookAsync(Book book)
        {
            using var conn = _db.CreateConnection();
            var sql = @"INSERT INTO Books (Title,Author,ISBN,Category,CopiesAvailable,PublishedYear,Status,IsDeleted)
VALUES (@Title,@Author,@ISBN,@Category,@CopiesAvailable,@PublishedYear,@Status,0); SELECT CAST(SCOPE_IDENTITY() as int);";
            return await conn.QuerySingleAsync<int>(sql, book);
        }

        public async Task UpdateBookAsync(Book book)
        {
            using var conn = _db.CreateConnection();
            var sql = @"UPDATE Books SET Title=@Title,Author=@Author,ISBN=@ISBN,Category=@Category,CopiesAvailable=@CopiesAvailable,PublishedYear=@PublishedYear,Status=@Status WHERE BookId=@BookId";
            await conn.ExecuteAsync(sql, book);
        }

        public async Task SoftDeleteBookAsync(int id)
        {
            using var conn = _db.CreateConnection();
            await conn.ExecuteAsync("UPDATE Books SET IsDeleted=1 WHERE BookId=@Id", new { Id = id });
        }

        // Members
        public async Task<IEnumerable<Member>> GetMembersAsync()
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryAsync<Member>("SELECT * FROM Members WHERE IsDeleted = 0");
        }
        public async Task<Member?> GetMemberByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Member>("SELECT * FROM Members WHERE MemberId=@Id AND IsDeleted=0", new { Id = id });
        }
        public async Task<int> AddMemberAsync(Member member)
        {
            using var conn = _db.CreateConnection();
            var sql = @"INSERT INTO Members (FullName,Email,Phone,JoinDate,IsActive,IsDeleted) VALUES (@FullName,@Email,@Phone,@JoinDate,@IsActive,0); SELECT CAST(SCOPE_IDENTITY() as int);";
            return await conn.QuerySingleAsync<int>(sql, member);
        }
        public async Task UpdateMemberAsync(Member member)
        {
            using var conn = _db.CreateConnection();
            await conn.ExecuteAsync("UPDATE Members SET FullName=@FullName,Email=@Email,Phone=@Phone,IsActive=@IsActive WHERE MemberId=@MemberId", member);
        }
        public async Task SoftDeleteMemberAsync(int id)
        {
            using var conn = _db.CreateConnection();
            await conn.ExecuteAsync("UPDATE Members SET IsDeleted=1 WHERE MemberId=@Id", new { Id = id });
        }

        // Borrowing
        public async Task<int> CreateBorrowAsync(Borrow borrow)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                var insertBorrow = @"INSERT INTO Borrows (MemberId,BorrowDate,DueDate,ReturnDate,PenaltyAmount,OverdueNotified) VALUES (@MemberId,@BorrowDate,@DueDate,NULL,NULL,0); SELECT CAST(SCOPE_IDENTITY() as int);";
                var borrowId = await conn.QuerySingleAsync<int>(insertBorrow, new { borrow.MemberId, borrow.BorrowDate, borrow.DueDate }, tran);

                foreach (var item in borrow.BorrowItems)
                {
                    // check copies
                    var copies = await conn.QueryFirstOrDefaultAsync<int?>("SELECT CopiesAvailable FROM Books WHERE BookId=@BookId AND IsDeleted=0", new { BookId = item.BookId }, tran);
                    if (copies == null) throw new InvalidOperationException($"Book id {item.BookId} not found.");
                    if (copies <= 0) throw new InvalidOperationException($"No copies available for book id {item.BookId}.");

                    // update book
                    await conn.ExecuteAsync("UPDATE Books SET CopiesAvailable=CopiesAvailable-1, Status = CASE WHEN CopiesAvailable-1 <=0 THEN 0 ELSE 1 END WHERE BookId=@BookId", new { BookId = item.BookId }, tran);

                    // insert borrow item
                    await conn.ExecuteAsync("INSERT INTO BorrowItems (BorrowId,BookId) VALUES (@BorrowId,@BookId)", new { BorrowId = borrowId, BookId = item.BookId }, tran);
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

        public async Task<Borrow?> GetBorrowByIdAsync(int id)
        {
            using var conn = _db.CreateConnection();
            var borrow = await conn.QueryFirstOrDefaultAsync<Borrow>("SELECT * FROM Borrows WHERE BorrowId=@Id", new { Id = id });
            if (borrow == null) return null;
            var items = await conn.QueryAsync<BorrowItem>("SELECT * FROM BorrowItems WHERE BorrowId=@BorrowId", new { BorrowId = id });
            borrow.BorrowItems = items.ToList();
            return borrow;
        }

        // Return with penalty calculation
        public async Task ReturnBorrowAsync(int borrowId, decimal perDayPenalty)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                var borrow = await conn.QueryFirstOrDefaultAsync<Borrow>("SELECT * FROM Borrows WHERE BorrowId=@Id", new { Id = borrowId }, tran);
                if (borrow == null) throw new InvalidOperationException("Borrow not found");
                if (borrow.ReturnDate != null) throw new InvalidOperationException("Already returned");

                var items = (await conn.QueryAsync<int>("SELECT BookId FROM BorrowItems WHERE BorrowId=@BorrowId", new { BorrowId = borrowId }, tran)).ToList();

                // update return date
                var returnDate = DateTime.UtcNow;
                await conn.ExecuteAsync("UPDATE Borrows SET ReturnDate=@ReturnDate WHERE BorrowId=@BorrowId", new { ReturnDate = returnDate, BorrowId = borrowId }, tran);

                // update book copies
                foreach (var bookId in items)
                {
                    await conn.ExecuteAsync("UPDATE Books SET CopiesAvailable = CopiesAvailable + 1, Status = CASE WHEN CopiesAvailable+1 > 0 THEN 1 ELSE 0 END WHERE BookId=@BookId", new { BookId = bookId }, tran);
                }

                // penalty calculation
                if (returnDate > borrow.DueDate)
                {
                    var daysLate = (int)Math.Ceiling((returnDate - borrow.DueDate).TotalDays);
                    var amount = (decimal)daysLate * perDayPenalty;
                    await conn.ExecuteAsync("INSERT INTO Penalties (BorrowId,DaysLate,Amount,CreatedAt) VALUES (@BorrowId,@DaysLate,@Amount,@CreatedAt)", new { BorrowId = borrowId, DaysLate = daysLate, Amount = amount, CreatedAt = DateTime.UtcNow }, tran);
                    await conn.ExecuteAsync("UPDATE Borrows SET PenaltyAmount=@Amount WHERE BorrowId=@BorrowId", new { Amount = amount, BorrowId = borrowId }, tran);
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public async Task<int> CountActiveBorrowsByMemberAsync(int memberId)
        {
            using var conn = _db.CreateConnection();
            return await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM Borrows WHERE MemberId=@MemberId AND ReturnDate IS NULL", new { MemberId = memberId });
        }

        public async Task<IEnumerable<Borrow>> GetBorrowsByDateRangeAsync(DateTime from, DateTime to)
        {
            using var conn = _db.CreateConnection();
            var borrows = (await conn.QueryAsync<Borrow>("SELECT * FROM Borrows WHERE BorrowDate >= @From AND BorrowDate <= @To", new { From = from, To = to })).ToList();
            if (!borrows.Any()) return borrows;
            var ids = borrows.Select(b => b.BorrowId).ToArray();
            var items = await conn.QueryAsync<BorrowItem>("SELECT * FROM BorrowItems WHERE BorrowId IN @Ids", new { Ids = ids });
            var lookup = items.ToLookup(i => i.BorrowId);
            foreach (var b in borrows) b.BorrowItems = lookup[b.BorrowId].ToList();
            return borrows;
        }
    }
}
