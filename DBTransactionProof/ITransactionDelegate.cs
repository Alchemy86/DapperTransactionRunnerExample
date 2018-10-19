
using System;
using System.Threading.Tasks;

namespace DBTransactionProof
{
    public interface ITransactionDelegate
    {
        Task DoWorkAsync(Func<Task> resultBody);
    }
}
