using System.Reflection;
using MARS.Platform.Infrastructure.Hosting.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace MARS.Platform.Infrastructure.Hosting.Extensions;

public static class MarsHubsSwaggerExtensions
{
    /// <summary>
    /// Имя документа Swashbuckle с описанием SignalR-хабов.
    /// </summary>
    public const string HubsDocumentName = "hubs";

    /// <summary>
    /// Добавляет второй Swagger-документ <c>hubs</c> с описанием SignalR-хабов.
    /// </summary>
    /// <param name="hubAssemblies">
    /// Сборки, в которых объявлены хабы и их callbacks-интерфейсы. По умолчанию
    /// SignalRSwaggerGen сканирует только entry assembly, а в MARS хабы лежат в
    /// отдельных проектах (например, <c>MARS.Alerts.Presentation.Overlay</c>),
    /// поэтому сборки надо передать явно.
    /// </param>
    /// <remarks>
    /// <para>
    /// Схема повторяет <c>MARS_old</c>: Swashbuckle отдаёт документы <c>v1</c> и
    /// <c>hubs</c>, а методы хабов попадают в <c>hubs</c> благодаря
    /// <c>AddSignalRSwaggerGen()</c> и атрибутам <c>[SignalRHub]</c> на классах
    /// хабов и интерфейсах callbacks.
    /// </para>
    /// <para>
    /// <see cref="PathPartitionDocumentFilter"/> делит пути между документами по
    /// <see cref="Swashbuckle.AspNetCore.SwaggerGen.DocumentFilterContext.DocumentName"/>;
    /// он нужен и сервисам с хабами, иначе пути <c>/hubs/*</c> уедут в REST-документ.
    /// </para>
    /// </remarks>
    public static WebApplicationBuilder AddMarsHubsSwagger(
        this WebApplicationBuilder builder,
        string serviceName,
        params Assembly[] hubAssemblies
    )
    {
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                HubsDocumentName,
                new OpenApiInfo { Title = $"{serviceName} Hubs", Version = "v1" }
            );

            options.AddSignalRSwaggerGen(signalR =>
            {
                if (hubAssemblies.Length > 0)
                {
                    signalR.ScanAssemblies(hubAssemblies);
                }

                // Только в документ hubs: в REST-документе хаб-путей быть не должно.
                signalR.DisplayInDocument(HubsDocumentName);
            });

            // Стандартные для MARS настройки схем: см. AddSwaggerServices в
            // MARS_old. Non-nullable ссылочные типы становятся обязательными,
            // перечисления — inline, чтобы генератор не создавал отдельные
            // enum-типы для каждого свойства.
            options.UseAllOfToExtendReferenceSchemas();
            options.SupportNonNullableReferenceTypes();
            options.NonNullableReferenceTypesAsRequired();
            options.UseInlineDefinitionsForEnums();

            options.DocumentFilter<DotNetTypesDocumentFilter>();

            // OperationResultSchemaFilter сознательно не регистрируется:
            // переписывание всех *OperationResult в allOf с x-generic-type-argument
            // меняет форму REST-контракта и схему сгенерированного клиента. Фильтр
            // перенесён из MARS_old как есть и включается отдельной задачей.
        });

        builder.Services.AddSwaggerGen(options =>
        {
            // Фильтр общий для обоих документов: он сам решает по имени документа,
            // какие пути оставить.
            options.DocumentFilter<PathPartitionDocumentFilter>();
        });

        return builder;
    }
}
