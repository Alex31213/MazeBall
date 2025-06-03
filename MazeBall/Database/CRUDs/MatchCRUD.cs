using MazeBall.Database.Entities;

namespace MazeBall.Database.CRUDs
{
    public class MatchCRUD : ICRUD<Match>
    {
        MazeBallContext dbContext;
        public MatchCRUD(MazeBallContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Add(Match addedMatch)
        {
            dbContext.Matches.Add(addedMatch);
            dbContext.SaveChanges();
        }
        public void Update(int matchId, Match updatedMatch)
        {
            var matches = dbContext.Matches;
            dbContext.Entry(matches.SingleOrDefault(x => x.MatchId == matchId)).CurrentValues.SetValues(updatedMatch);
            dbContext.SaveChanges();
        }
        public Match GetMatchByMatchId(int matchId)
        {
            List<Match> matches = dbContext.Matches.ToList();
            return matches.SingleOrDefault(x => x.MatchId == matchId);
        }
        public List<Match> GetMatchesByCreationDate(DateTime creationDate)
        {
            List<Match> matches = dbContext.Matches.ToList();
            return matches.FindAll(x => x.CreationDate == creationDate).Select(x => x).ToList();
        }
    }
}
