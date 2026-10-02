using MARS.Admin.Services;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// <see cref="OperationResult"/> — конверт, который возвращают контроллеры
/// админки. Проверяются фабрики и неявные операторы: на них держится проверка
/// «успех» в вызывающем коде.
/// </summary>
public class OperationResultTests
{
    [Fact]
    public void OkCarriesMessageAndData()
    {
        var result = OperationResult.Ok("готово", new { Id = 1 });

        Assert.True(result.Success);
        Assert.Equal("готово", result.Message);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public void BadIsAlwaysFailure()
    {
        var result = OperationResult.Bad("не вышло", "данные");

        Assert.False(result.Success);
        Assert.Equal("не вышло", result.Message);
        Assert.Equal("данные", result.Data);
    }

    [Fact]
    public void DefaultInstanceIsFailure()
    {
        var result = new OperationResult();

        Assert.False(result.Success);
        Assert.Null(result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public void PropertiesAreMutable()
    {
        var result = OperationResult.Bad();

        result.Success = true;
        result.Message = "исправлено";
        result.Data = 42;

        Assert.True(result.Success);
        Assert.Equal("исправлено", result.Message);
        Assert.Equal(42, result.Data);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SomeKeepsGivenFlag(bool success)
    {
        Assert.Equal(success, OperationResult.Some(success, "сообщение", "данные").Success);
    }

    [Fact]
    public void SomeDefaultsToFailure()
    {
        Assert.False(OperationResult.Some().Success);
    }

    [Fact]
    public void NegationFollowsSuccessFlag()
    {
        Assert.False(!OperationResult.Ok());
        Assert.True(!OperationResult.Bad());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TruthinessFollowsSuccessFlag(bool success)
    {
        var result = success ? OperationResult.Ok() : OperationResult.Bad();

        var truthy = result ? true : false;

        Assert.Equal(success, truthy);
    }

    [Fact]
    public void FalseOperatorIsExactOppositeOfTrue()
    {
        var ok = OperationResult.Ok();
        var bad = OperationResult.Bad();

        Assert.True(ok ? true : false);
        Assert.False(!ok ? true : false);
        Assert.False(bad ? true : false);
        Assert.True(!bad ? true : false);
    }

    [Fact]
    public void GenericOkCarriesTypedData()
    {
        var result = OperationResult<int>.Ok("посчитано", 7);

        Assert.True(result.Success);
        Assert.Equal(7, result.Data);
        Assert.Equal("посчитано", result.Message);
        Assert.True(result ? true : false);
    }

    [Fact]
    public void GenericBadKeepsDataForDiagnostics()
    {
        var result = OperationResult<string>.Bad("нет данных", "fallback");

        Assert.False(result.Success);
        Assert.Equal("fallback", result.Data);
        Assert.False(result ? true : false);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GenericSomeKeepsGivenFlag(bool success)
    {
        Assert.Equal(success, OperationResult<int>.Some(success, "сообщение", 1).Success);
    }

    /// <summary>
    /// Неявное приведение отдаёт данные без проверки успеха: вызывающий код
    /// обязан смотреть на флаг сам, иначе пустой результат уедет дальше как
    /// настоящее значение.
    /// </summary>
    [Fact]
    public void GenericResultConvertsToItsData()
    {
        OperationResult<string> result = OperationResult<string>.Ok("готово", "данные");

        string value = result;

        Assert.Equal("данные", value);
    }
}
