using Microsoft.EntityFrameworkCore;

namespace HomeHeatMap.Data
{
    public interface ICrimeDataRepository
    {
        Task<CrimeCity?> GetCityAsync(string city, string state);
        Task<List<CrimeCity>> GetAllCitiesAsync();
    }

    public class CrimeDataRepository : ICrimeDataRepository
    {
        private readonly AppDbContext _db;

        public CrimeDataRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CrimeCity?> GetCityAsync(string city, string state)
        {
            return await _db.CrimeCities
                .Where(c => c.City.ToLower() == city.ToLower() &&
                            c.State.ToLower() == state.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<List<CrimeCity>> GetAllCitiesAsync()
        {
            return await _db.CrimeCities
                .OrderBy(c => c.State)
                .ThenBy(c => c.City)
                .ToListAsync();
        }
    }
}