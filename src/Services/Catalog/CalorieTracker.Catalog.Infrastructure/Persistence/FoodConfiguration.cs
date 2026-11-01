using CalorieTracker.Catalog.Domain.Foods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalorieTracker.Catalog.Infrastructure.Persistence;

internal sealed class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    private const int BrandLength = 200;
    private const int BarcodeLength = 14;
    private const int SourceLength = 32;
    private const int SourceIdLength = 64;
    private const int NutrientPrecision = 6;
    private const int GramsPrecision = 7;
    private const int OneDecimal = 1;

    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.ToTable("Foods");
        builder.HasKey(food => food.Id);

        builder.Property(food => food.Name).HasMaxLength(Food.MaxNameLength).IsRequired();
        builder.Property(food => food.Brand).HasMaxLength(BrandLength);
        builder.HasIndex(food => food.Name).HasMethod("gin").HasOperators("gin_trgm_ops");

        builder.Property(food => food.Barcode)
            .HasConversion(barcode => barcode!.Value, value => Barcode.Create(value).Value)
            .HasMaxLength(BarcodeLength);
        builder.HasIndex(food => food.Barcode).IsUnique();

        builder.Property(food => food.Source).HasConversion<string>().HasMaxLength(SourceLength);
        builder.Property(food => food.SourceId).HasMaxLength(SourceIdLength).IsRequired();
        builder.HasIndex(food => new { food.Source, food.SourceId }).IsUnique();

        builder.ComplexProperty(food => food.Per100Grams, nutrients =>
        {
            nutrients.Property(n => n.Calories).HasColumnName("Calories").HasPrecision(NutrientPrecision, OneDecimal);
            nutrients.Property(n => n.Protein).HasColumnName("Protein").HasPrecision(NutrientPrecision, OneDecimal);
            nutrients.Property(n => n.Carbohydrates).HasColumnName("Carbohydrates").HasPrecision(NutrientPrecision, OneDecimal);
            nutrients.Property(n => n.Fat).HasColumnName("Fat").HasPrecision(NutrientPrecision, OneDecimal);
        });

        builder.Property(food => food.ServingDescription).HasMaxLength(Serving.MaxDescriptionLength);
        builder.Property(food => food.ServingGrams).HasPrecision(GramsPrecision, OneDecimal);
        builder.Ignore(food => food.DefaultServing);
    }
}
