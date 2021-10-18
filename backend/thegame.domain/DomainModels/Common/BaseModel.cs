using System;
using System.Collections.Generic;

namespace thegame.domain.DomainModels.Common
{
    public class BaseModel
    {
        private HashSet<BaseDomainEvent> _domainEvents;
        public IReadOnlyCollection<BaseDomainEvent> DomainEvents => _domainEvents;

        public DateTimeOffset? CreatedOn { get; }
        public long? CreatedBy { get; }
        public DateTimeOffset? ModifiedOn { get; }
        public long? ModifiedBy { get; }


    }
}