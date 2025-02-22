using DevDash.model;
using DevDash.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace DevDash.Repository
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        private AppDbContext _db;
        public ProjectRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<Project> UpdateAsync(Project project)
        {
            _db.Projects.Update(project);
            await _db.SaveChangesAsync();
            return project;
        }
        public async Task RemoveAsync(Project project)
        {
            var commentsToRemove = await _db.Comments.Where(u => u.ProjectId == project.Id).ToListAsync();
            _db.Comments.RemoveRange(commentsToRemove);

            var IssuesToRemove = await _db.Issues.Where(u => u.ProjectId == project.Id).ToListAsync();
            _db.Issues.RemoveRange(IssuesToRemove);

            var SprintsToRemove = await _db.Sprints.Where(u => u.ProjectId == project.Id).ToListAsync();
            _db.Sprints.RemoveRange(SprintsToRemove);

            var UserProjectToRemove = await _db.UserProjects.Where(u => u.ProjectId == project.Id).ToListAsync();
            _db.UserProjects.RemoveRange(UserProjectToRemove);

            _db.Projects.Remove(project);

            await _db.SaveChangesAsync();
        }

    }
}
