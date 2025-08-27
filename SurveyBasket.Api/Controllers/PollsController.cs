namespace SurveyBasket.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollsController(IPollService pollService) : ControllerBase
    {
         private readonly IPollService _pollService = pollService;

        [HttpGet("")]
        public IActionResult GetAll()
        {
            var polls = _pollService.GetAll();
            var response = polls.Adapt<IEnumerable<PollResponse>>();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult Get([FromRoute]int id)
        {
            var poll = _pollService.Get(id);
            return poll is null ? NotFound() : Ok(poll.Adapt<PollResponse>());      
        }

        [HttpPost("")]
        public IActionResult Add([FromBody] CreatePollRequest request)
        {
           var newPoll = _pollService.Add(request.Adapt<Poll>());
            return CreatedAtAction(nameof(Get), new { id = newPoll.Id }, newPoll);
        }

        [HttpPut("{id}")]
        public IActionResult Update([FromRoute]int id ,[FromBody] CreatePollRequest request) 
        {
           var isUpdated =  _pollService.Update(id, request.Adapt<Poll>());
            if(!isUpdated) return NotFound();
            return  NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete([FromRoute] int id)
        {
            var isDeleted = _pollService.Delete(id);
            if (!isDeleted) return NotFound();
            return NoContent();
        }
    }
}
 