namespace TheGame.Domain.DomainModels.LicensePlate
{
  public class LicensePlateModel
  {
    public long Id { get; protected set; }

    public StateOrProvince StateOrProvince { get; protected set; }

    public Country Country { get; protected set; }
  }

  //TODO Populate
  public enum StateOrProvince
  {
    // US
    CA,

    // Canada
    BritishColumbia
  }

  public enum Country
  {
    US,
    CA
  }
}
