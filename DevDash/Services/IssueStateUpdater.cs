using Microsoft.EntityFrameworkCore;
namespace DevDash.Services
{
    public class IssueStateUpdater
    {
        public readonly AppDbContext _context;
        public IssueStateUpdater(AppDbContext context)
        {
            _context = context;
        }

        public async Task UpdateIssueStateAsync()
        {
            

            var today = DateTime.Today;

            var issues = await _context.Issues
                .Where(i => i.Deadline.HasValue && i.Deadline < today && i.Status != "Canceled" && i.Status!= "Reviewing" && i.Status != "Completed")
                .ToListAsync();
            foreach (var issue in issues)
            {
                issue.Status = "Canceled";
                _context.Issues.Update(issue);
                
            }
            await _context.SaveChangesAsync();


        }

        public async Task UpdateIssueAsync()
        {


            var today = DateTime.Today;

            var issues = await _context.Issues
                .Where(i => i.Deadline.HasValue && i.DeliveredDate < today && i.Status != "Reviewing")
                .ToListAsync();
            foreach (var issue in issues)
            {
                issue.Status = "Reviewing";
                _context.Issues.Update(issue);

            }
            await _context.SaveChangesAsync();


        }
    }

}
