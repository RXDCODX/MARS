using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MARS.Platform.Infrastructure.Hosting.Swagger;

/// <summary>
/// Делит операции между документами: REST-пути остаются в документе API,
/// пути хабов — в документе <c>hubs</c>.
/// </summary>
/// <remarks>
/// В MARS документы называются <c>v1</c> (REST) и <c>hubs</c> (SignalR), поэтому
/// признак берётся из <see cref="DocumentFilterContext.DocumentName"/>. В старом
/// репозитории различали по <c>Info.Title</c> («Telegramus API» / «Telegramus
/// Hubs»), но здесь title документа равен имени сервиса и про hubs ничего не
/// говорит — эвристика по title не сработала бы и хаб-пути остались бы в обоих
/// документах.
/// </remarks>
public sealed class PathPartitionDocumentFilter : IDocumentFilter
{
    private const string HubsDocumentName = "hubs";

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Paths is null)
        {
            return;
        }

        var isHubsDoc = string.Equals(
            context.DocumentName,
            HubsDocumentName,
            StringComparison.OrdinalIgnoreCase
        );

        var keep = new Dictionary<string, IOpenApiPathItem>(StringComparer.OrdinalIgnoreCase);
        var tagsKeep = new Dictionary<string, OpenApiTag>(StringComparer.OrdinalIgnoreCase);

        foreach (var (path, value) in swaggerDoc.Paths)
        {
            if (IsHubPath(path) == isHubsDoc)
            {
                keep[path] = value;
            }
        }

        if (swaggerDoc.Tags is not null)
        {
            foreach (var openApiTag in swaggerDoc.Tags)
            {
                if (
                    !string.IsNullOrEmpty(openApiTag.Name)
                    && IsHubTag(openApiTag.Name) == isHubsDoc
                )
                {
                    tagsKeep[openApiTag.Name] = openApiTag;
                }
            }
        }

        swaggerDoc.Paths.Clear();
        foreach (var (path, value) in keep)
        {
            swaggerDoc.Paths.Add(path, value);
        }

        if (swaggerDoc.Tags is not null)
        {
            swaggerDoc.Tags.Clear();
            foreach (var tag in tagsKeep.Values)
            {
                swaggerDoc.Tags.Add(tag);
            }
        }

        // Оставляем только схемы, на которые ссылаются оставшиеся пути хабов:
        // без этого в документ hubs уезжает весь REST-контракт сервиса, и
        // генератор клиента строит лишние модели. REST-документ не трогаем —
        // хаб-схем в нём нет, и его форма не должна меняться из-за этой правки.
        if (isHubsDoc && swaggerDoc.Components?.Schemas is { Count: > 0 } schemas)
        {
            var referenced = CollectReferencedSchemas(swaggerDoc);
            var toRemove = schemas.Keys.Where(name => !referenced.Contains(name)).ToList();
            foreach (var name in toRemove)
            {
                schemas.Remove(name);
            }
        }
    }

    private static bool IsHubPath(string path)
    {
        return path.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHubTag(string name)
    {
        return name.EndsWith("hub", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string> CollectReferencedSchemas(OpenApiDocument doc)
    {
        var referenced = new HashSet<string>(StringComparer.Ordinal);

        void EnqueueSchema(IOpenApiSchema? schema)
        {
            if (schema is null)
            {
                return;
            }

            if (schema is OpenApiSchemaReference { Reference.Id: { Length: > 0 } id })
            {
                referenced.Add(id);
                return;
            }

            if (schema is not OpenApiSchema concreteSchema)
            {
                return;
            }

            if (concreteSchema.Items is not null)
            {
                EnqueueSchema(concreteSchema.Items);
            }

            if (concreteSchema.Not is not null)
            {
                EnqueueSchema(concreteSchema.Not);
            }

            if (concreteSchema.AllOf is not null)
            {
                foreach (var nested in concreteSchema.AllOf)
                {
                    EnqueueSchema(nested);
                }
            }

            if (concreteSchema.AnyOf is not null)
            {
                foreach (var nested in concreteSchema.AnyOf)
                {
                    EnqueueSchema(nested);
                }
            }

            if (concreteSchema.OneOf is not null)
            {
                foreach (var nested in concreteSchema.OneOf)
                {
                    EnqueueSchema(nested);
                }
            }

            if (concreteSchema.AdditionalProperties is not null)
            {
                EnqueueSchema(concreteSchema.AdditionalProperties);
            }

            if (concreteSchema.Properties is not null)
            {
                foreach (var property in concreteSchema.Properties.Values)
                {
                    EnqueueSchema(property);
                }
            }
        }

        foreach (var path in doc.Paths.Values)
        {
            if (path.Operations is null)
            {
                continue;
            }

            foreach (var operation in path.Operations.Values)
            {
                if (operation.Parameters is not null)
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        EnqueueSchema(parameter.Schema);
                    }
                }

                if (operation.RequestBody?.Content is not null)
                {
                    foreach (var mediaType in operation.RequestBody.Content.Values)
                    {
                        EnqueueSchema(mediaType.Schema);
                    }
                }

                if (operation.Responses is null)
                {
                    continue;
                }

                foreach (var response in operation.Responses.Values)
                {
                    if (response.Content is null)
                    {
                        continue;
                    }

                    foreach (var mediaType in response.Content.Values)
                    {
                        EnqueueSchema(mediaType.Schema);
                    }
                }
            }
        }

        // Транзитивное замыкание по components: схема может ссылаться на схему,
        // которую не видно из операций напрямую.
        if (doc.Components?.Schemas is not null)
        {
            var queue = new Queue<string>(referenced);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (queue.Count > 0)
            {
                var name = queue.Dequeue();
                if (!visited.Add(name))
                {
                    continue;
                }

                if (!doc.Components.Schemas.TryGetValue(name, out var schema))
                {
                    continue;
                }

                var before = referenced.Count;
                EnqueueSchema(schema);
                if (referenced.Count > before)
                {
                    foreach (var newly in referenced.Where(x => !visited.Contains(x)))
                    {
                        queue.Enqueue(newly);
                    }
                }
            }
        }

        return referenced;
    }
}
