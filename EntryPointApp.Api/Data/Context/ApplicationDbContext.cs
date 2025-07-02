using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Data.Context
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        
    }
}