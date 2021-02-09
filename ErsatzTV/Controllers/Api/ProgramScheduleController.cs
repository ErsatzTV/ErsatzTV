using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ErsatzTV.Application.ProgramSchedules;
using ErsatzTV.Application.ProgramSchedules.Commands;
using ErsatzTV.Application.ProgramSchedules.Queries;
using ErsatzTV.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers.Api
{
    [ApiController]
    [Route("api/schedules")]
    [Produces("application/json")]
    public class ProgramScheduleController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProgramScheduleController(IMediator mediator) => _mediator = mediator;

        [HttpPost]
        [ProducesResponseType(typeof(ProgramScheduleViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Add(
            [Required] [FromBody]
            CreateProgramSchedule createProgramSchedule) =>
            _mediator.Send(createProgramSchedule).ToActionResult();

        [HttpGet("{programScheduleId}")]
        [ProducesResponseType(typeof(ProgramScheduleViewModel), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> Get(int programScheduleId) =>
            _mediator.Send(new GetProgramScheduleById(programScheduleId)).ToActionResult();

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProgramScheduleViewModel>), 200)]
        public Task<IActionResult> GetAll() =>
            _mediator.Send(new GetAllProgramSchedules()).ToActionResult();

        [HttpPatch]
        [ProducesResponseType(typeof(ProgramScheduleViewModel), 200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Update(
            [Required] [FromBody]
            UpdateProgramSchedule updateProgramSchedule) =>
            _mediator.Send(updateProgramSchedule).ToActionResult();

        [HttpDelete]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public Task<IActionResult> Delete(
            [Required] [FromBody]
            DeleteProgramSchedule deleteProgramSchedule) =>
            _mediator.Send(deleteProgramSchedule).ToActionResult();

        [HttpGet("{programScheduleId}/items")]
        [ProducesResponseType(typeof(IEnumerable<ProgramScheduleItemViewModel>), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> GetItems(int programScheduleId) =>
            _mediator.Send(new GetProgramScheduleItems(programScheduleId)).ToActionResult();

        [HttpPut("{programScheduleId}/items")]
        [ProducesResponseType(typeof(IEnumerable<ProgramScheduleItemViewModel>), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> PutItems(
            int programScheduleId,
            [Required] [FromBody]
            List<ReplaceProgramScheduleItem> items) =>
            _mediator.Send(new ReplaceProgramScheduleItems(programScheduleId, items)).ToActionResult();

        [HttpDelete("{programScheduleId}/items")]
        [ProducesResponseType(typeof(IEnumerable<ProgramScheduleItemViewModel>), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> DeleteItems(int programScheduleId) =>
            _mediator.Send(new ReplaceProgramScheduleItems(programScheduleId, new List<ReplaceProgramScheduleItem>()))
                .ToActionResult();

        [HttpPost("items/add")]
        [ProducesResponseType(typeof(IEnumerable<ProgramScheduleItemViewModel>), 200)]
        [ProducesResponseType(404)]
        public Task<IActionResult> AddItem(
            [Required] [FromBody]
            AddProgramScheduleItem addProgramScheduleItem) =>
            _mediator.Send(addProgramScheduleItem).ToActionResult();
    }
}
