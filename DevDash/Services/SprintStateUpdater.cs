using Microsoft.EntityFrameworkCore;
namespace DevDash.Services
{
    public class SprintStateUpdater
    {
        public readonly AppDbContext _context;
        public SprintStateUpdater(AppDbContext context)
        {
            _context = context;
        }
        public async Task UpdateSprintStateAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today); 
                                                              
            var sprints = await _context.Sprints
                .Where(s => s.EndDate.HasValue && s.EndDate.Value < today && s.Status != "Canceled" && s.Status != "Completed")
                .ToListAsync();
            foreach (var sprint in sprints)
            {
                sprint.Status = "Canceled"; 
                _context.Sprints.Update(sprint);   
            }
            await _context.SaveChangesAsync(); 
        }
    }
}
