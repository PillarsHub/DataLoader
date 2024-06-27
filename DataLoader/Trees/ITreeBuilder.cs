using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLoader.Trees
{
    interface ITreeBuilder
    {
        Task BuildTree(Dictionary<string, string> virtualTree, bool deleteExisting);
        Dictionary<string, string> BuildVirtualTree(int numberOfNodes);
        Task AddPlacements(IEnumerable<Placement> nodes);
    }
}
