using FluentAssertions;

namespace Sds.Storage.Blob.Specs.StepDefinitions
{
    [Binding]
    class ConfigurationSteps
    {
        private readonly TestExecutionResult _testExecutionResult;

        public ConfigurationSteps(TestExecutionResult testExecutionResult)
        {
            _testExecutionResult = testExecutionResult;
        }

        [Then(@"the application\.json is used for configuration")]
        public void ThenTheApp_ConfigIsUsedForConfiguration()
        {
            _testExecutionResult.ExecutionLog.Should().Contain("Using application.json");
        }
    }
}
