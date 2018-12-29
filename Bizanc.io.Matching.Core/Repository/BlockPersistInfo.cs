using System;
using Bizanc.io.Matching.Core.Domain;

namespace Bizanc.io.Matching.Core.Repository
{
    public class BlockPersistInfo
    {
        public string Id { get; set; }
        
        public DateTime TimeStamp { get; set; }
        public string BlockHash { get; set; }
    }
}