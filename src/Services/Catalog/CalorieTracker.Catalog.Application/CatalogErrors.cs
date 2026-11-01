using CalorieTracker.BuildingBlocks.Results;

namespace CalorieTracker.Catalog.Application;

public static class CatalogErrors
{
    public static readonly Error FoodNotFound =
        Error.NotFound("Catalog.FoodNotFound", "We couldn't find this food.");

    public static readonly Error SearchTermTooShort =
        Error.Validation("Catalog.SearchTermTooShort", "Type at least 2 characters to search.");

    public static readonly Error InvalidLimit =
        Error.Validation("Catalog.InvalidLimit", "The number of results must be between 1 and 50.");
}
