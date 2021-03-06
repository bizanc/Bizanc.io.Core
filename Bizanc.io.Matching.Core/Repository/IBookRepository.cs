using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Domain.Immutable;

namespace Bizanc.io.Matching.Core.Repository
{
    public interface IBookRepository: IBaseRepository<Book>
    {
        Task<List<Book>> Get();

        Task Clean();
    }
}