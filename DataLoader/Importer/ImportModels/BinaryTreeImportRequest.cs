namespace Importer.Contracts;

public class BinaryTreeImportRequest
{
    public string NodeId { get; set; } = "";
    public string UplineId { get; set; } = "";
    public string Leg { get; set; } = "";
    public DateTime? EffectiveDate { get; set; } = null;
}
