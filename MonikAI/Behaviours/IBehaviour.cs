namespace MonikAI.Behaviours
{
    /*
     * Implement this interface to make your own Behaviours. No registering is needed, every class that inherits from this will automatically be loaded!
     */
    public interface IBehaviour
    {
        void Init(MainWindow window);
        void Update(MainWindow window);
    }
}