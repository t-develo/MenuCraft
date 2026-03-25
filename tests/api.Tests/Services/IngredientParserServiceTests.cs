using FluentAssertions;
using MenuCraft.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MenuCraft.Api.Tests.Services;

public class IngredientParserServiceTests
{
    private static IngredientParserService CreateService() =>
        new(NullLogger<IngredientParserService>.Instance);

    // --- Basic parsing ---

    [Fact]
    public void ParseIngredients_WithNameAndQuantityAndUnit_ParsesCorrectly()
    {
        var service = CreateService();

        var result = service.ParseIngredients("鶏もも肉 200 g");

        result.Ingredients.Should().HaveCount(1);
        var ingredient = result.Ingredients[0];
        ingredient.Name.Should().Be("鶏もも肉");
        ingredient.Quantity.Should().Be("200");
        ingredient.Unit.Should().Be("g");
    }

    [Fact]
    public void ParseIngredients_WithNameOnly_ParsesNameWithoutQuantityOrUnit()
    {
        var service = CreateService();

        var result = service.ParseIngredients("塩");

        result.Ingredients.Should().HaveCount(1);
        var ingredient = result.Ingredients[0];
        ingredient.Name.Should().Be("塩");
        ingredient.Quantity.Should().BeNull();
        ingredient.Unit.Should().BeNull();
    }

    [Fact]
    public void ParseIngredients_MultipleLines_ParsesEachLine()
    {
        var service = CreateService();

        var result = service.ParseIngredients("鶏もも肉 200 g\n玉ねぎ 1 個\n醤油 大さじ 2");

        result.Ingredients.Should().HaveCount(3);
        result.Ingredients[0].Name.Should().Be("鶏もも肉");
        result.Ingredients[0].Quantity.Should().Be("200");
        result.Ingredients[0].Unit.Should().Be("g");
        result.Ingredients[1].Name.Should().Be("玉ねぎ");
        result.Ingredients[1].Quantity.Should().Be("1");
        result.Ingredients[1].Unit.Should().Be("個");
        result.Ingredients[2].Name.Should().Be("醤油");
        result.Ingredients[2].Quantity.Should().Be("大さじ");
        result.Ingredients[2].Unit.Should().Be("2");
    }

    [Fact]
    public void ParseIngredients_WithJapaneseVolumeUnits_ParsesCorrectly()
    {
        var service = CreateService();

        var lines = "砂糖 大さじ 1\n醤油 小さじ 2\n牛乳 カップ 1";
        var result = service.ParseIngredients(lines);

        result.Ingredients.Should().HaveCount(3);
        result.Ingredients[0].Name.Should().Be("砂糖");
        result.Ingredients[0].Quantity.Should().Be("大さじ");
        result.Ingredients[0].Unit.Should().Be("1");
        result.Ingredients[1].Name.Should().Be("醤油");
        result.Ingredients[1].Quantity.Should().Be("小さじ");
        result.Ingredients[1].Unit.Should().Be("2");
        result.Ingredients[2].Name.Should().Be("牛乳");
        result.Ingredients[2].Quantity.Should().Be("カップ");
        result.Ingredients[2].Unit.Should().Be("1");
    }

    [Fact]
    public void ParseIngredients_WithEmptyLines_SkipsEmptyLines()
    {
        var service = CreateService();

        var result = service.ParseIngredients("鶏もも肉 200 g\n\n  \n玉ねぎ 1 個");

        result.Ingredients.Should().HaveCount(2);
    }

    [Fact]
    public void ParseIngredients_WithEmptyText_ReturnsEmptyList()
    {
        var service = CreateService();

        var result = service.ParseIngredients("");

        result.Ingredients.Should().BeEmpty();
    }

    [Fact]
    public void ParseIngredients_WithWhitespaceOnlyText_ReturnsEmptyList()
    {
        var service = CreateService();

        var result = service.ParseIngredients("   \n  \n ");

        result.Ingredients.Should().BeEmpty();
    }

    [Fact]
    public void ParseIngredients_WithDecimalQuantity_ParsesCorrectly()
    {
        var service = CreateService();

        var result = service.ParseIngredients("バター 0.5 個");

        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Quantity.Should().Be("0.5");
        result.Ingredients[0].Unit.Should().Be("個");
    }

    [Fact]
    public void ParseIngredients_WithFractionQuantity_ParsesCorrectly()
    {
        var service = CreateService();

        var result = service.ParseIngredients("牛乳 1/2 カップ");

        result.Ingredients.Should().HaveCount(1);
        result.Ingredients[0].Quantity.Should().Be("1/2");
        result.Ingredients[0].Unit.Should().Be("カップ");
    }

    [Fact]
    public void ParseIngredients_WithVariousUnits_ParsesCorrectly()
    {
        var service = CreateService();

        var lines = "にんじん 1 本\nベーコン 3 枚\nほうれん草 1 束\nトマト缶 1 缶\n水 500 ml\nお米 2 カップ";
        var result = service.ParseIngredients(lines);

        result.Ingredients.Should().HaveCount(6);
        result.Ingredients[0].Unit.Should().Be("本");
        result.Ingredients[1].Unit.Should().Be("枚");
        result.Ingredients[2].Unit.Should().Be("束");
        result.Ingredients[3].Unit.Should().Be("缶");
        result.Ingredients[4].Unit.Should().Be("ml");
        result.Ingredients[5].Unit.Should().Be("カップ");
    }
}
