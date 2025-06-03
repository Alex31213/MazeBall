using MazeBall.Database.Entities;

namespace MazeBall.Database.CRUDs
{
    public class MatchResultCRUD : ICRUD<MatchResult>
    {
        MazeBallContext dbContext;
        public MatchResultCRUD(MazeBallContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public void Add(MatchResult addedMatchResult)
        {
            dbContext.MatchResults.Add(addedMatchResult);
            dbContext.SaveChanges();
        }
        public void Update(string username, int matchId, MatchResult updatedMatchResult)
        {
            var matchResults = dbContext.MatchResults;
            dbContext.Entry(matchResults.SingleOrDefault(x => x.MatchId == matchId && x.Username == username))
                .CurrentValues.SetValues(updatedMatchResult);
            dbContext.SaveChanges();
        }
        public int CountUsernameMatches(string username)
        {
            return dbContext.MatchResults.Count(x => x.Username == username);
        }
        public int CountUsernameWins(string username)
        {
            return dbContext.MatchResults.Count(x => x.Username == username && x.Result == true);
        }
        public int CountUsernameLosses(string username)
        {
            return dbContext.MatchResults.Count(x => x.Username == username && x.Result == false);
        }
    }
}
