namespace TheGame.Domain.DomainModels.Common;

/// <summary>
/// Enumeration entity marker interface
/// </summary>
/// <remarks>
/// This entity type has a finite set of values that should generally be unchanged.
/// Enumeration pattern allows for cleaner DDD experience that eliminates unnecessary DB trips.
/// For this pattern to work properly, project implementation must ensure the following:
/// 1. Entity definition to not have ANY navigation props or collection (these can cause obscure duplicate change tracking issues).
/// 2. SaveChangesAsync must force IEnumeration entity states to be Unmodified to eliminate unecessary sql update statements.
/// </remarks>
public interface IEnumeration { }
