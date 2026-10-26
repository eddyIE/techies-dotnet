using Techies.Web.Infrastructure;

namespace Techies.Tests.Unit;

public class SlugTests
{
    [Theory]
    [InlineData("Điện thoại Samsung", "dien-thoai-samsung")]
    [InlineData("  Tivi   Sony 75\" 4K ", "tivi-sony-75-4k")]
    [InlineData("iPhone 15 Pro Max – 256GB", "iphone-15-pro-max-256gb")]
    [InlineData("Phụ kiện & Đồ chơi", "phu-kien-do-choi")]
    public void From_strips_vietnamese_marks_and_punctuation(string input, string expected) =>
        Assert.Equal(expected, Slug.From(input));
}
