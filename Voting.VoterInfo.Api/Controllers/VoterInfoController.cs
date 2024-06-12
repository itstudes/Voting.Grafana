using Bogus;
using Microsoft.AspNetCore.Mvc;
using Voting.VoterInfo.Api.Models;
using Voting.VoterInfo.Models;

namespace Voting.VoterInfo.Api.Controllers;

[ApiController]
[Route("voter")]
public class VoterInfoController : ControllerBase
{
    private readonly Faker _faker;
    private readonly ILogger<VoterInfoController> _logger;

    public VoterInfoController(ILogger<VoterInfoController> logger)
    {
        _logger = logger;
        _faker = new Faker();
    }

    [HttpGet]
    public IActionResult GetVoterInformation([FromQuery(Name = "idNumber")]string idNumber)
    {
        try
        {
            //checks
            //non-null
            if (string.IsNullOrEmpty(idNumber))
            {
                return BadRequest("Voter ID number is required.");
            }
            //only numbers in ID
            if (!idNumber.All(char.IsDigit))
            {
                return BadRequest("Voter ID number must only contain numbers.");
            }
            //correct length
            if (idNumber.Length != 13)
            {
                return BadRequest("Voter ID number must be 13 digits long.");
            }

            //get voter info
            //use bogus to generate a voter name
            var returnData = new VoterInfoWebModel
            {
                IdNumber = idNumber,
                FullName = _faker.Name.FullName(),
                Address = _faker.Address.FullAddress(),
                EmailAddress = _faker.Internet.Email(),
                PhoneNumber = _faker.Phone.PhoneNumber()
            };

            //log information request
            _logger.LogInformation($"Voter information requested for ID number: {idNumber}");

            //return data
            return Ok(returnData);
        }
        catch (Exception ex)
        {
            //log error
            _logger.LogError(ex, "An error occurred while getting voter information.");

            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            //return HTTP 500 error
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }
}
