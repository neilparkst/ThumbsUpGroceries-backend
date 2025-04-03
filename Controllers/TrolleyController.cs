using Microsoft.AspNetCore.Mvc;
using ThumbsUpGroceries_backend.Data;

namespace ThumbsUpGroceries_backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TrolleyController : ControllerBase
    {
        private readonly IDataRepository _dataRepository;

        public TrolleyController(IDataRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }
    }
}
