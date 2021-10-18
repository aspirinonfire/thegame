namespace thegame.domain.DomainModels
{
    public class LicensePlate
    {
        public long Id { get; set; }

        public StateOrProvince StateOrProvince { get; }

        public Country Country  { get; }        
    }

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