using System.Threading.Tasks;

namespace LiquidClock.Sample
{
    public interface IService
    {
        Task<string> Read();
    }
}
