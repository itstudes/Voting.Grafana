using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Voting.Grafana.Controllers;

[ApiController]
[Route("test")]
public class K6LoadTestController : ControllerBase
{
    private readonly ILogger<K6LoadTestController> _logger;
    private readonly K6TestManager _testManager;
    private readonly ActivitySource _activitySource;

    public K6LoadTestController(ILogger<K6LoadTestController> logger,
                                AppInstrumentation appInstrumentation,
                                K6TestManager testManager)
    {
        _logger = logger;
        _activitySource = appInstrumentation.ActivitySource;
        _testManager = testManager;
    }

    [HttpPost("start")]
    public IActionResult StartK6Test([FromBody] K6TestRequest testRequest)
    {
        using var activity = _activitySource.StartActivity("K6LoadTestController.StartK6Test");
        try
        {
            //checks
            if (string.IsNullOrEmpty(testRequest.ScriptName))
            {
                return BadRequest("The script name must be provided.");
            }

            //try run test
            var successfullyRun = _testManager.TryRunK6Test(testRequest.ScriptName,
                                                            out var testId,
                                                            out var results);
            if (!successfullyRun)
            {
                return Conflict(results);
            }

            //return data
            var returnData = new K6TestStatus
            {
                IsTestRunning = true,
                OngoingTestId = testId
            };
            return Ok(returnData);
        }
        catch (Exception ex)
        {
            //set failure activity status
            activity?.SetStatus(ActivityStatusCode.Error);

            //log error
            _logger.LogError(ex, "An error occurred while trying to start a k6 test.");

            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }

    [HttpGet("status")]
    public IActionResult GetCurrentK6TestStatus()
    {
        //get data
        var isTestRunning = _testManager.IsTestRunning;
        Guid? ongoingTestId = null;
        if (isTestRunning)
        {
            ongoingTestId = _testManager.OngoingTestId;
        }

        var returnData = new K6TestStatus
        {
            IsTestRunning = isTestRunning,
            OngoingTestId = ongoingTestId
        };
        return Ok(returnData);
    }

    [HttpGet("results/{testId}")]
    public IActionResult GetResultsForK6Test(Guid testId)
    {
        using var activity = _activitySource.StartActivity("K6LoadTestController.GetResultsForK6Test");
        try
        {
            //try get results
            var results = _testManager.GetTestResults(testId);
            if (string.IsNullOrEmpty(results))
            {
                return NotFound();
            }

            //return results
            var returnData = new K6TestResult
            {
                TestId = testId,
                Results = results
            };
            return Ok(returnData);
        }
        catch (Exception ex)
        {
            //set failure activity status
            activity?.SetStatus(ActivityStatusCode.Error);

            //log error
            _logger.LogError(ex, "An error occurred while trying to get the results for a k6 test (testId={testId}).", testId);

            //return HTTP 500 error
            var exceptionModel = new InternalExceptionWebModel
            {
                Code = ex.HResult.ToString(),
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            };
            return StatusCode(StatusCodes.Status500InternalServerError, exceptionModel);
        }
    }
}
