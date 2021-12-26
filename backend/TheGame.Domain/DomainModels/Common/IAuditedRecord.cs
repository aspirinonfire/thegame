using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGame.Domain.DomainModels.Common
{
  /// <summary>
  /// Record with creation and modification dates
  /// </summary>
  public interface IAuditedRecord
  {
    public DateTimeOffset DateCreated { get; }
    public DateTimeOffset? DateModified { get; }
  }
}
