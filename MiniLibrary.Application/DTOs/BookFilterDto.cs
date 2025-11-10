using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniLibrary.Application.DTOs
{
    public record BookFilterDto(string? Title, string? Category, string? ISBN, int Page = 1, int PageSize = 10);
    //public record BookFilterDto(string? Title, string? Category, string? ISBN);

}
