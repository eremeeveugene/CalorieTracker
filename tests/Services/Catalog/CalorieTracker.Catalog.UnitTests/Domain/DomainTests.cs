using CalorieTracker.Catalog.Domain.Foods;

namespace CalorieTracker.Catalog.UnitTests.Domain;

public class BarcodeTests
{
    [Theory]
    [InlineData("3017624010701", "3017624010701")]
    [InlineData("009800800124", "0009800800124")]
    [InlineData("96385074", "96385074")]
    [InlineData(" 3017624010701 ", "3017624010701")]
    public void Create_ValidBarcode_KeepsPrintedForm(string input, string printedForm)
    {
        var barcode = Barcode.Create(input);

        barcode.IsSuccess.ShouldBeTrue();
        barcode.Value.PrintedForm.ShouldBe(printedForm);
    }

    [Fact]
    public void Create_UpcAndEan13OfSameProduct_AreEqual()
    {
        var upc = Barcode.Create("009800800124").Value;
        var ean13 = Barcode.Create("0009800800124").Value;

        upc.ShouldBe(ean13);
    }

    [Theory]
    [InlineData("3017624010702")]
    [InlineData("301762401070")]
    [InlineData("30176240107A1")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidBarcode_Fails(string? input)
    {
        Barcode.Create(input).Error.ShouldBe(FoodErrors.InvalidBarcode);
    }
}

public class NutrientsTests
{
    [Fact]
    public void Create_RoundsToOneDecimal()
    {
        var nutrients = Nutrients.Create(88.96m, 1.09m, 22.84m, 0.33m).Value;

        nutrients.ShouldBe(Nutrients.Create(89m, 1.1m, 22.8m, 0.3m).Value);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(100, -0.1, 0, 0)]
    [InlineData(950, 0, 0, 100)]
    [InlineData(400, 40, 40, 30)]
    public void Create_ImpossibleValues_Fail(decimal calories, decimal protein, decimal carbohydrates, decimal fat)
    {
        Nutrients.Create(calories, protein, carbohydrates, fat).Error.ShouldBe(FoodErrors.InvalidNutrients);
    }

    [Fact]
    public void Create_PureFat_IsValid()
    {
        Nutrients.Create(900m, 0m, 0m, 100m).IsSuccess.ShouldBeTrue();
    }
}

public class FoodTests
{
    private static readonly Nutrients BananaNutrients = Nutrients.Create(89m, 1.1m, 22.8m, 0.3m).Value;
    private static readonly FoodOrigin Origin = new(FoodSource.Usda, "173944");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_Fails(string name)
    {
        Food.Create(name, null, null, Origin, BananaNutrients, null, DateTimeOffset.UtcNow).Error.ShouldBe(FoodErrors.InvalidName);
    }

    [Fact]
    public void Create_WithNonPositiveServing_Fails()
    {
        Food.Create("Bananas, raw", null, null, Origin, BananaNutrients, new Serving("1 medium", 0m), DateTimeOffset.UtcNow)
            .Error.ShouldBe(FoodErrors.InvalidServing);
    }

    [Fact]
    public void Create_TrimsAndKeepsServing()
    {
        var food = Food.Create("  Bananas, raw ", " ", null, Origin, BananaNutrients, new Serving("1 medium", 118m), DateTimeOffset.UtcNow).Value;

        food.Name.ShouldBe("Bananas, raw");
        food.Brand.ShouldBeNull();
        food.DefaultServing.ShouldBe(new Serving("1 medium", 118m));
    }
}
