using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace MARS.MediaStorage.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeLegacyMemeOrderPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Строки с MemeTypeId = NULL — алерт-ассеты (suprise.webm, cringe.mp4 и т. п.),
            // попавшие в пул random-meme из монолита. Playback выбирает мемы только по
            // MemeTypeId, поэтому эти строки недостижимы; алерты уже живут в таблице Alerts
            // с относительными путями. Удаляем как legacy-мусор.
            migrationBuilder.Sql(
                """
                DELETE FROM mediastorage."RandomMemeOrder"
                WHERE "MemeTypeId" IS NULL;
                """
            );

            // 2. Абсолютные пути монолита (D:\VS\MARS_old\...\wwwroot\Alerts\...) не
            // резолвятся в контейнере. Приводим к относительному виду (Alerts/...),
            // как в таблице Alerts и как ожидает MemeMediaPreparationService.ResolveFullPath.
            migrationBuilder.Sql(
                """
                UPDATE mediastorage."RandomMemeOrder"
                SET "FilePath" = replace(
                    substring("FilePath" from strpos(lower("FilePath"), 'wwwroot') + 8),
                    '\',
                    '/'
                )
                WHERE "FilePath" ILIKE '%wwwroot%';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Данные-миграция: обратное восстановление абсолютных путей монолита
            // и удалённых строк не имеет смысла (исходные пути привязаны к машине
            // разработчика и не воспроизводимы). Откат = no-op.
        }
    }
}
