using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common
{
  public static class CollectionHelpers
  {
    public static HashSet<T> GetWriteableCollection<T>(this IEnumerable<T> navCollection)
    {
      if (navCollection == null)
      {
        return [];
      }

      if (navCollection is HashSet<T> writeable)
      {
        return writeable;
      }
      return new HashSet<T>(navCollection);
    }
  }
}