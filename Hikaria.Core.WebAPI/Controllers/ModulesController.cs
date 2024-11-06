using AutoMapper;
using Hikaria.Core.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Hikaria.Core.WebAPI.Controllers
{
    [Route("api/gtfo/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase
    {
        private readonly ILogger<ModulesController> _logger;
        private readonly IRepositoryWrapper _repository;
        private readonly IMapper _mapper;

        public ModulesController(IRepositoryWrapper repository, ILogger<ModulesController> logger, IMapper mapper)
        {
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }
    }
}
