using System.Threading.Tasks;

namespace Bizanc.io.Matching.Core.Domain
{
    public interface IPeerService
    {
        Task Process(IPeer peer);
    }
}