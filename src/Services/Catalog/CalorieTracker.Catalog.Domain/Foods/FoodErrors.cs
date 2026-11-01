using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Catalog.Domain.Foods;

public static class FoodErrors
{
    public static readonly Error InvalidBarcode =
        Error.Validation("Catalog.InvalidBarcode", "This isn't a valid product barcode.");

    public static readonly Error InvalidNutrients =
        Error.Validation("Catalog.InvalidNutrients", "Nutrition values must be non-negative and add up to at most 100 g per 100 g.");

    public static readonly Error InvalidName =
        Error.Validation("Catalog.InvalidName", $"A food needs a name of at most {Food.MaxNameLength} characters.");

    public static readonly Error InvalidServing =
        Error.Validation("Catalog.InvalidServing", "A serving needs a description and a positive weight.");
}
