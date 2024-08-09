using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.Dtos;
using PlatformService.Models;
using PlatformService.SyncDataServices.Http;

namespace PlatformService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PlatformsController : ControllerBase
{
    private readonly IPlatformRepo _repository;
    private readonly IMapper _mapper;
    private readonly ICommandDataClient _dataClient;
    private readonly IMessageBusClient _messageBus;

    public PlatformsController(IPlatformRepo repository,
        IMapper mapper,
        ICommandDataClient dataClient,
        IMessageBusClient messageBus)
    {
        _repository = repository;
        _mapper = mapper;
        _dataClient = dataClient;
        _messageBus = messageBus;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PlatformReadDto>> GetPlatforms()
    {
        var platforms = _repository.GetAllPlatforms();
        return Ok(_mapper.Map<IEnumerable<PlatformReadDto>>(platforms));
    }

    [HttpGet("{id}", Name = "GetPlatformById")]
    public ActionResult<PlatformReadDto> GetPlatformById(int id)
    {
        var platform = _repository.GetPlatformById(id);
        if (platform != null)
        {
            return Ok(_mapper.Map<PlatformReadDto>(platform));
        }
        return NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<PlatformReadDto>> CreatePlatform(PlatformCreateDto platformCreateDto)
    {
        var platformModel = _mapper.Map<Platform>(platformCreateDto);
        _repository.CreatePlatform(platformModel);
        _repository.SaveChanges();

        var platformReadDto = _mapper.Map<PlatformReadDto>(platformModel);

        // send sync message
        try
        {
            await _dataClient.SendPlatformToCommand(platformReadDto);
        }
        catch (Exception ex) 
        {
            Console.WriteLine($"Could not send synchronously {ex.Message}");
        }

        //send async message
        try
        {
            var platformPublishDto = _mapper.Map<PlatformPublishedDto>(platformReadDto);
            platformPublishDto.Event = "Platform_Published";
            _messageBus.PublishNewPlatform(platformPublishDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"---> could not send message asynchronously {ex.Message}");
        }

        return CreatedAtRoute(nameof(GetPlatformById), new { Id = platformReadDto.Id }, platformReadDto);
    }
}
