using Microsoft.EntityFrameworkCore;
using Reply.API.Models;

namespace Reply.API.Data
{
    public class ReplyContext(DbContextOptions<ReplyContext> options) : DbContext(options)
    {
        public DbSet<ReplyItem> Replies { get; set; }
    }
}
