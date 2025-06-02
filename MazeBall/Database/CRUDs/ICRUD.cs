namespace MazeBall.Database.CRUDs
{
    public interface ICRUD<T> where T : class
    {
        public void Add(T Added);
    }
}
