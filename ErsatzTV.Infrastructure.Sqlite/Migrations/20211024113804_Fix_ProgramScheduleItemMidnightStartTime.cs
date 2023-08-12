using Microsoft.EntityFrameworkCore.Migrations;

namespace ErsatzTV.Infrastructure.Sqlite.Migrations
{
    public partial class Fix_ProgramScheduleItemMidnightStartTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "update ProgramScheduleItem set StartTime = '00:00:00' where StartTime = '1.00:00:00'");

            migrationBuilder.Sql(
@"delete from PlayoutItem where PlayoutId in
(select Playout.Id from Playout
inner join ProgramSchedule PS on Playout.ProgramScheduleId = PS.Id
inner join ProgramScheduleItem PSI on PSI.ProgramScheduleId = PS.Id
where PSI.StartTime = '00:00:00')");

            migrationBuilder.Sql(
                @"delete from PlayoutProgramScheduleAnchor where PlayoutId in
(select Playout.Id from Playout
inner join ProgramSchedule PS on Playout.ProgramScheduleId = PS.Id
inner join ProgramScheduleItem PSI on PSI.ProgramScheduleId = PS.Id
where PSI.StartTime = '00:00:00')");

            migrationBuilder.Sql(
                @"UPDATE Playout SET Anchor_NextStart = null, Anchor_NextScheduleItemId = null where Id in
(select Playout.Id from Playout
inner join ProgramSchedule PS on Playout.ProgramScheduleId = PS.Id
inner join ProgramScheduleItem PSI on PSI.ProgramScheduleId = PS.Id
where PSI.StartTime = '00:00:00')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
