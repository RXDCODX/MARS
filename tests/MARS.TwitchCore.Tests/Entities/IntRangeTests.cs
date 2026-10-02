using MARS.TwitchCore.Entities.Subs;

namespace MARS.TwitchCore.Tests.Entities;

/// <summary>
/// Числовой диапазон в ответе мини-игры.
///
/// Диапазон показывается зрителю, поэтому важны обе границы включительно и вид
/// числа: положительное печатается со знаком «+», иначе подсказка выглядела бы
/// как «[-5]», хотя означает «пять очков».
/// </summary>
public class IntRangeTests
{
    [Theory]
    [InlineData(1, 5, 1, true)]
    [InlineData(1, 5, 5, true)]
    [InlineData(1, 5, 3, true)]
    [InlineData(1, 5, 0, false)]
    [InlineData(1, 5, 6, false)]
    public void ContainsRespectsBothBorders(int start, int end, int value, bool expected)
    {
        var range = new IntRange(start, end);

        Assert.Equal(expected, range.Contains(value));
    }

    [Fact]
    public void LengthCountsBothBorders()
    {
        Assert.Equal(5, new IntRange(1, 5).Length);
        Assert.Equal(1, new IntRange(3, 3).Length);
    }

    [Fact]
    public void SingleNumberIsPrintedWithoutRange()
    {
        Assert.Equal("[+3]", new IntRange(3, 3).ToString());
    }

    [Fact]
    public void RangeIsPrintedWithTilde()
    {
        Assert.Equal("[+1~+5]", new IntRange(1, 5).ToString());
    }

    /// <summary>
    /// Отрицательное число печатается со своим знаком и без плюса: иначе
    /// «[-2~-1]» читалось бы как «от минус двух до минус один», что и нужно,
    /// но «[-2]» не должен превращаться в «[+-2]».
    /// </summary>
    [Fact]
    public void NegativeNumbersKeepTheirSign()
    {
        Assert.Equal("[-2]", new IntRange(-2, -2).ToString());
        Assert.Equal("[-3~-1]", new IntRange(-3, -1).ToString());
    }

    /// <summary>
    /// Ноль печатается без знака: условие печатает «+» только для положительных,
    /// и «[+0~+1]» в подсказке выглядело бы опечаткой.
    /// </summary>
    [Fact]
    public void ZeroIsPrintedWithoutPlus()
    {
        Assert.Equal("[0~+1]", new IntRange(0, 1).ToString());
    }
}
