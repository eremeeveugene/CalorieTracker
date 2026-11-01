using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Catalog.Domain.Foods;

// A GS1 product barcode (EAN-8, UPC-A, EAN-13 or GTIN-14). Stored as 14 digits, so the same
// product matches whether it was scanned as UPC-A or as EAN-13.
public sealed record Barcode
{
    private const int Gtin14Length = 14;
    private const string Ean8Padding = "000000";

    private static readonly int[] ValidLengths = [8, 12, 13, 14];

    private Barcode(string gtin14)
    {
        Value = gtin14;
    }

    public string Value { get; }

    // The form printed on packages and used by food databases: EAN-8, EAN-13 or GTIN-14
    public string PrintedForm
    {
        get
        {
            if (Value.StartsWith(Ean8Padding, StringComparison.Ordinal))
            {
                return Value[Ean8Padding.Length..];
            }

            return Value[0] == '0' ? Value[1..] : Value;
        }
    }

    public static Result<Barcode> Create(string? input)
    {
        var digits = input?.Trim() ?? string.Empty;
        if (!ValidLengths.Contains(digits.Length) || !digits.All(char.IsAsciiDigit) || !HasValidCheckDigit(digits))
        {
            return Result.Failure<Barcode>(FoodErrors.InvalidBarcode);
        }

        return Result.Success(new Barcode(digits.PadLeft(Gtin14Length, '0')));
    }

    public override string ToString() => PrintedForm;

    // GS1 check digit: data digits are weighted 3, 1, 3, 1… starting from the rightmost one
    private static bool HasValidCheckDigit(string digits)
    {
        var sum = 0;
        var weight = 3;
        for (var index = digits.Length - 2; index >= 0; index--)
        {
            sum += (digits[index] - '0') * weight;
            weight = 4 - weight;
        }

        return (10 - (sum % 10)) % 10 == digits[^1] - '0';
    }
}
