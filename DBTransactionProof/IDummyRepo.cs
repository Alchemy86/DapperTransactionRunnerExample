using System.Threading.Tasks;

namespace DBTransactionProof
{
    public interface IDummyRepo
    {
        Task<int> DoCommit(bool fail, string name);
    }
}
