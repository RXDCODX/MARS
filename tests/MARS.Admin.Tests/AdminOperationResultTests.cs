using MARS.Admin.Entities;
using MARS.Admin.Services;
using Xunit;

namespace MARS.Admin.Tests;

/// <summary>
/// Результат операции в панели управления.
///
/// Панель читает успех из тела ответа, а не из кода: проверяется, что признак и
/// данные доезжают вместе и что типизированный результат отдаёт данные без
/// приведения.
/// </summary>
public class AdminOperationResultTests
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
    public void BadIsFailure()
    {
        var result = OperationResult.Bad("не получилось");

        Assert.False(result.Success);
        Assert.Equal("не получилось", result.Message);
    }

    /// <summary>
    /// Условные операторы следуют за признаком: проверка в контроллере должна
    /// означать «успешно».
    /// </summary>
    [Fact]
    public void OperatorsFollowTheFlag()
    {
        Assert.True(!OperationResult.Bad());
        Assert.False(!OperationResult.Ok());
    }

    /// <summary>
    /// Булев контекст оценивается через <c>op_True</c>/<c>op_False</c>: «если
    /// результат» обязано означать «успешно», иначе панель начнёт обрабатывать
    /// ошибку как успех.
    /// </summary>
    [Fact]
    public void BooleanContextFollowsTheFlag()
    {
        if (OperationResult.Bad("нет"))
        {
            Assert.Fail("Bad считается успехом в булевом контексте");
        }
    }

    /// <summary>
    /// Частичный успех остаётся успехом: панель показывает его как результат с
    /// предупреждением.
    /// </summary>
    [Fact]
    public void SomeKeepsBothFlags()
    {
        Assert.True(OperationResult.Some(success: true, "частично").Success);
        Assert.False(OperationResult.Some(success: false, "нет").Success);
    }

    [Fact]
    public void TypedResultCarriesData()
    {
        var result = OperationResult<int>.Ok("готово", 42);

        Assert.True(result.Success);
        Assert.Equal(42, result.Data);
        Assert.Equal(42, (int)result);
    }
}
