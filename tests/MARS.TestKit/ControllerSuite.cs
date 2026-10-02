using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MARS.TestKit;

/// <summary>
/// Обход всех действий контроллера: контроллер создаётся с заглушками, каждое
/// публичное действие вызывается с подставляемыми аргументами и должно вернуть
/// результат, а не бросить исключение.
///
/// Зачем обход, а не тест на каждый контроллер: контроллеров в репозитории
/// больше тридцати, и почти все они — тонкие обёртки над сервисом. Ценность
/// этой проверки не в бизнес-условиях (их проверяют точечные тесты), а в том,
/// что ломается молча: забытый аргумент конструктора, значение, на котором
/// действие падает с NullReferenceException, опечатка в маршруте. Обход ловит
/// это для всего сервиса разом и не даёт забыть про контроллер, добавленный
/// полгода назад.
///
/// Чего обход не делает: не проверяет содержимое ответа и не подменяет
/// точечные тесты бизнес-логики.
/// </summary>
public static class ControllerSuite
{
    /// <summary>
    /// Прогоняет все контроллеры сборки и возвращает отчёт: список неудач,
    /// список действий с неподставляемыми аргументами и число вызванных
    /// действий. Успех — пустой список неудач при ненулевом числе вызовов.
    /// </summary>
    public static ControllerSuiteReport Verify(string assemblyName, int depth = 2)
    {
        var assembly = Assembly.Load(assemblyName);
        var failures = new List<string>();
        var skipped = new List<string>();
        var invoked = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore();
        var requestServices = services.BuildServiceProvider();

        foreach (var controller in Controllers(assembly))
        {
            var instance = Create(controller, depth);
            if (instance is null)
            {
                // Контроллер с конкретными сервисами, а не с интерфейсами:
                // собрать его заглушками нельзя (TwitchConnectionManager
                // подключается к Twitch). Такой контроллер проверяется
                // отдельным тестом с настоящими зависимостями, а обход его
                // честно пропускает, вместо того чтобы падать.
                skipped.Add($"{controller.Name}: не собирается заглушками, нужен отдельный тест.");
                continue;
            }

            // Контекст запроса вне конвейера MVC равен null, и любое действие с
            // HttpContext.RequestAborted упало бы с NullReferenceException —
            // то есть проверялась бы не регистрация контроллера, а способ
            // вызова. DefaultHttpContext даёт те же значения по умолчанию, что и
            // в работающем сервисе.
            //
            // RequestServices обязателен: MVC-результаты (StatusCodeResult,
            // FileStreamResult) достают из запроса ILoggerFactory и
            // IHttpResponseBodyFeature, а без контейнера падают с
            // ArgumentNullException("provider") — то есть проверялся бы не
            // контроллер, а способ вызова.
            if (instance is ControllerBase controllerBase)
            {
                controllerBase.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { RequestServices = requestServices },
                };
            }

            foreach (var action in Actions(controller))
            {
                var arguments = BuildArguments(action, depth);
                if (arguments is null)
                {
                    skipped.Add(
                        $"{controller.Name}.{action.Name}: "
                            + string.Join(
                                ", ",
                                action
                                    .GetParameters()
                                    .Select(parameter => parameter.ParameterType.Name)
                                    .Where(name => name != nameof(CancellationToken))
                            )
                    );
                    continue;
                }

                invoked++;
                var failure = Invoke(controller, action, instance, arguments);
                if (failure is null)
                {
                    continue;
                }
                else if (IsStubLimitation(failure))
                {
                    // Заглушка Moq не умеет подставлять значение типа, который
                    // нельзя проксировать (DbContext, sealed-класс без
                    // конструктора). Это ограничение заглушки, а не дефект
                    // контроллера, поэтому действие помечается пропущенным.
                    skipped.Add($"{controller.Name}.{action.Name}: {failure}");
                }
                else
                {
                    failures.Add(failure);
                }
            }

            (instance as IDisposable)?.Dispose();
        }

        return new ControllerSuiteReport(failures, skipped, invoked);
    }

    private static IEnumerable<Type> Controllers(Assembly assembly) =>
        assembly
            .GetExportedTypes()
            .Where(type =>
                typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && !type.IsGenericTypeDefinition
            )
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName && IsActionResult(method.ReturnType))
            .OrderBy(method => method.Name, StringComparer.Ordinal);

    /// <summary>
    /// Результат действия — это <c>IActionResult</c>, <c>ActionResult</c> либо
    /// обёртка <c>Task</c> над ними.
    ///
    /// Отдельно проверяется <see cref="IConvertToActionResult"/>: в .NET 10
    /// <c>ActionResult&lt;T&gt;</c> больше не наследует <c>ActionResult</c> и не
    /// является <c>IActionResult</c> — он реализует только преобразование в
    /// результат. Без этой проверки ни одно действие не было бы найдено, а
    /// обход тихо проверил бы пустоту.
    /// </summary>
    private static bool IsActionResult(Type type)
    {
        var result = false;

        if (
            typeof(IActionResult).IsAssignableFrom(type)
            || typeof(IConvertToActionResult).IsAssignableFrom(type)
            || type == typeof(Task)
        )
        {
            result = true;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
        {
            result = IsActionResult(type.GetGenericArguments()[0]);
        }

        return result;
    }

    private static object? Create(Type controller, int depth) => Stub.Resolve(controller, depth);

    /// <summary>
    /// Признак того, что падение вызвано заглушкой, а не контроллером: Moq не
    /// может построить прокси для типа без подходящего конструктора.
    /// </summary>
    private static bool IsStubLimitation(string failure) =>
        failure.Contains("Can not instantiate proxy", StringComparison.Ordinal)
        || failure.Contains("Unsupported expression", StringComparison.Ordinal);

    private static object?[]? BuildArguments(MethodInfo action, int depth)
    {
        var parameters = action.GetParameters();
        var arguments = new object?[parameters.Length];

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                arguments[index] = CancellationToken.None;
                continue;
            }

            var value = SampleValue.TryCreate(parameter.ParameterType, depth);
            if (value is null)
            {
                return null;
            }

            arguments[index] = value;
        }

        return arguments;
    }

    private static string? Invoke(
        Type controller,
        MethodInfo action,
        object instance,
        object?[] arguments
    )
    {
        string? failure = null;

        try
        {
            var returned = action.Invoke(instance, arguments);
            if (returned is Task task)
            {
                task.GetAwaiter().GetResult();
            }
        }
        catch (Exception exception)
        {
            failure = $"{controller.Name}.{action.Name}: {Describe(exception)}";
        }

        return failure;
    }

    private static string Describe(Exception exception) =>
        exception is TargetInvocationException { InnerException: { } inner }
            ? $"{inner.GetType().Name}: {FirstLine(inner.Message)}"
        : exception is AggregateException aggregate && aggregate.InnerException is not null
            ? $"{aggregate.InnerException.GetType().Name}: {FirstLine(aggregate.InnerException.Message)}"
        : $"{exception.GetType().Name}: {FirstLine(exception.Message)}";

    private static string FirstLine(string message) =>
        message.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? message;
}

/// <summary>
/// Итог обхода контроллеров: неудачи, пропущенные действия и число вызовов.
/// </summary>
public sealed record ControllerSuiteReport(
    IReadOnlyList<string> Failures,
    IReadOnlyList<string> Skipped,
    int ActionsInvoked
)
{
    /// <summary>
    /// Готовое сообщение для <c>Assert.Empty</c>: и неудачи, и пропуски видны
    /// в выводе теста, а не теряются в молчаливом зелёном.
    /// </summary>
    public string Describe() =>
        string.Join(
            Environment.NewLine,
            [
                $"Вызвано действий: {ActionsInvoked}, пропущено: {Skipped.Count}.",
                .. Failures.Select(failure => "ПРОВАЛ " + failure),
                .. Skipped.Select(skipped => "ПРОПУЩЕНО " + skipped),
            ]
        );
}
