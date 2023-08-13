using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Migrate_TailFiller_FillerPreset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
insert into FillerPreset (Name, FillerKind, FillerMode, CollectionType, MediaItemId, CollectionId, MultiCollectionId, SmartCollectionId)
select
       'Migrated_Filler_' || TailCollectionType || '_' || ifnull(TailCollectionId, '') || '_' || ifnull(TailMediaItemId, '') || '_' || ifnull(TailMultiCollectionId, '') || '_' || ifnull(TailSmartCollectionId, ''),
       4,
       0,
       TailCollectionType,
       TailMediaItemId,
       TailCollectionId,
       TailMultiCollectionId,
       TailSmartCollectionId
from (select distinct TailCollectionType, TailCollectionId, TailMediaItemId, TailMultiCollectionId, TailSmartCollectionId from ProgramScheduleDurationItem)");

            migrationBuilder.Sql(
                @"
update ProgramScheduleItem
set TailFillerId = FPID
from (select fp.Id as FPID, psdi.Id as PSDIID from FillerPreset fp inner join ProgramScheduleDurationItem psdi where TailCollectionType = CollectionType and TailCollectionId is CollectionId and TailMediaItemId is MediaItemId and TailMultiCollectionId is MultiCollectionId and TailSmartCollectionId is SmartCollectionId) as whatever
where PSDIID = ProgramScheduleItem.Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
