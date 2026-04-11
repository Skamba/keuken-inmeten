namespace Keuken_inmeten.Models;

public class PaneelVerificatieStatus
{
    public Guid ToewijzingId { get; set; }
    public bool MatenOk { get; set; }
    public bool ScharnierPositiesOk { get; set; }
}
