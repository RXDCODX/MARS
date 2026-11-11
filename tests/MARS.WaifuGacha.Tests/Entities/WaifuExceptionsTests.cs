using MARS.WaifuGacha.Domain.Entities;

namespace MARS.WaifuGacha.Tests.Entities;

public sealed class WaifuExceptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Недостаточно ресурсов")]
    public void RollExceptionPreservesMessage(string message)
    {
        var exception = new WaifuRollException(message);

        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Невозможно объединить персонажей")]
    public void MergeExceptionPreservesMessage(string message)
    {
        var exception = new WaifuMergeException(message);

        Assert.Equal(message, exception.Message);
    }
}
