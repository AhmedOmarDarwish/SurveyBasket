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
            return Ok(_pollService.GetAll());
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var poll = _pollService.Get(id);
            return poll is null ? NotFound() : Ok(poll);
        }

        [HttpPost("")]
        public IActionResult Add(Poll request) { 
            var newPoll = _pollService.Add(request);
            return CreatedAtAction(nameof(Get), new {id = newPoll.Id}, newPoll );
        
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, Poll request)
        {
            var isUpdated = _pollService.Update(id, request);
            return isUpdated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return _pollService.Delete(id) ? NoContent() : NotFound();
        }


    }
}
