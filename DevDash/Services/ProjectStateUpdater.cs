using Microsoft.EntityFrameworkCore;
namespace DevDash.Services
{
    public class ProjectStateUpdater
    {
        public readonly AppDbContext _context;
        public ProjectStateUpdater(AppDbContext context)
        {
            _context = context;
        }
        public async Task UpdateProjectStateAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var projects = await _context.Projects
                .Where(s => s.EndDate.HasValue && s.EndDate.Value < today && s.Status != "Canceled")
                .ToListAsync();
            foreach (var project in projects)
            {
                project.Status = "Canceled";
                _context.Projects.Update(project);
            }
            await _context.SaveChangesAsync();
        }
    }
}
