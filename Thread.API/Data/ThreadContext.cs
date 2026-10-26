using Microsoft.EntityFrameworkCore;
using Thread.API.Models;

namespace Thread.API.Data;

public class ThreadContext(DbContextOptions<ThreadContext> options) : DbContext(options)
{
    public DbSet<Models.Thread> Threads => Set<Models.Thread>();
}
