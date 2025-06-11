using MazeBall.Database.Entities;

namespace MazeBall.Database.CRUDs
{
    public class UserCRUD : ICRUD<User>
    {
        MazeBallContext dbContext;
        public UserCRUD(MazeBallContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Add(User addedUser)
        {
            dbContext.Users.Add(addedUser);
            dbContext.SaveChanges();
        }
        public void Update(string username, User updatedUser)
        {
            var users = dbContext.Users;
            dbContext.Entry(users.SingleOrDefault(x => x.Username == username)).CurrentValues.SetValues(updatedUser);
            dbContext.SaveChanges();
        }
        public void ChangeDeletedState(string username, bool isDeleted)
        {
            var users = dbContext.Users;
            User updatedUser = users.SingleOrDefault(x => x.Username == username);
            updatedUser.Active = isDeleted;
            dbContext.Entry(users.SingleOrDefault(x => x.Username == username)).CurrentValues.SetValues(updatedUser);
            dbContext.SaveChanges();
        }
        public List<string> GetAllUsersUsernames()
        {
            List<User> users = dbContext.Users.ToList();
            return users.FindAll(x => x.Active == true).Select(x => x.Username).ToList();
        }
        public User GetUserByUsername(string username)
        {
            List<User> users = dbContext.Users.ToList();
            return users.SingleOrDefault(x => x.Username == username);
        }

        public User GetUserByEmail(string email)
        {
            List<User> users = dbContext.Users.ToList();
            return users.SingleOrDefault(x => x.Email == email);
        }

        public void ChangePassword(string username, string hashedNewPassword)
        {
            var users = dbContext.Users;
            User updatedUser = users.SingleOrDefault(x => x.Username == username);
            updatedUser.Password = hashedNewPassword;
            dbContext.Entry(users.SingleOrDefault(x => x.Username == username)).CurrentValues.SetValues(updatedUser);
            dbContext.SaveChanges();
        }
    }
}
