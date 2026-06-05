namespace Medika.Domain.Finance;

public record InvoiceId(Guid Value)
{
    public static InvoiceId New() => new(Guid.NewGuid());
    public static InvoiceId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}
