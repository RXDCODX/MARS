using MARS.Discord.Models;

namespace MARS.Discord.Tests.Models;

/// <summary>
/// Результат операции Discord.
///
/// Проверяется, что успех и ошибка различаются и что результат можно использовать
/// как условие: контроллеры Discord отдают наружу именно этот тип, и ошибочная
/// трактовка <c>Success</c> превратила бы ошибку в успешный ответ клиенту.
/// </summary>
public class OperationResultTests
{
    [Fact]
    public void OkIsSuccessWithData()
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
    /// Условные операторы следуют за флагом: проверка <c>if (result)</c> должна
    /// означать «успешно».
    /// </summary>
    [Fact]
    public void OperatorsFollowSuccessFlag()
    {
        var ok = OperationResult.Ok();
        var bad = OperationResult.Bad();

        Assert.True(ok.Success);
        Assert.False(bad.Success);
        Assert.True(!bad);
        Assert.False(!ok);

        // op_False вызывается через && в логическом И
        var left = bad;
        var right = OperationResult.Ok();
        var _ = left && right;
    }

    /// <summary>
    /// Операторы истинности применяются именно в условии: это и есть проверка
    /// результата в контроллерах, а <c>Assert</c> компилируется в обычный bool и
    /// операторы не зовёт.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConditionUsesTruthinessOperators(bool success)
    {
        var result = success ? OperationResult.Ok() : OperationResult.Bad();
        var takenSuccessBranch = false;

        if (result)
        {
            takenSuccessBranch = true;
        }
        else
        {
            takenSuccessBranch = false;
        }

        Assert.Equal(success, takenSuccessBranch);
    }

    /// <summary>
    /// Данные типизированного результата доступны без приведения: сервисы Discord
    /// читают <c>Data</c> напрямую.
    /// </summary>
    [Fact]
    public void TypedResultCarriesData()
    {
        var result = OperationResult<string>.Ok("готово", "данные");

        Assert.True(result.Success);
        Assert.Equal("данные", result.Data);
        Assert.Equal("данные", (string)result);
    }

    [Fact]
    public void TypedBadIsFailure()
    {
        var result = OperationResult<int>.Bad("ошибка");

        Assert.False(result.Success);
        Assert.Equal(0, result.Data);
    }

    /// <summary>
    /// Неуспешный левый результат останавливает логическое И и сохраняет
    /// исходную ошибку, не подменяя её правым результатом.
    /// </summary>
    [Fact]
    public void LogicalAndUsesFalseOperator()
    {
        var result = OperationResult.Bad("первая ошибка");
        var next = OperationResult.Ok();

        var value = result && next;

        Assert.Same(result, value);
        Assert.False(value.Success);
        Assert.Equal("первая ошибка", value.Message);
    }

    [Fact]
    public void LogicalAndReturnsRightResultWhenLeftSucceeded()
    {
        var result = OperationResult.Ok();
        var next = OperationResult.Bad("следующая ошибка");

        var value = result && next;

        Assert.Same(next, value);
        Assert.False(value.Success);
        Assert.Equal("следующая ошибка", value.Message);
    }
}
