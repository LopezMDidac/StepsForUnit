# How to create test steps for any Unit test framework

Since unit test frameworks are quite used for integration and e2e test. it is convenient to for most of our test frameworks to intervent in more atomic way, for evidence collection, for metric reading among other functionalities.

This repository shows an example about how to create test steps inside a Unit test framework that doesn't support this functionality by default.

# Tech stack
.NET8
MSTests

# Example

This example builds a Test case with a generic test step chain. To ilustrate the concept, the step chain automatically builds a report with the execution team of each step. That is cool because normally the test framework only provides total execution time of the test case without the detail about how long is taking each step.

This example will show the benefits to use this kind of architecture leading to a clean test case design and transparently get powerfull insights about test execution. Test execution is just a meaningful example but this approach offer the possibility to implement a different behaviour

# The Code

1. Define the StepChain Interface

 ```cs
    IStepChain Step(Action action);
    IStepChain Step(Action<dynamic[]> action, params dynamic[] args);
    IStepChain Step(string name, Action action);
    IStepChain Step(string name, Action<dynamic[]> action, params dynamic[] args);
    IStepChain Step<T>(Func<dynamic[], T> function, out T returnValue, params dynamic[] args);
    IStepChain Step<T>(Func<T> function, out T returnValue, params dynamic[] args);
    IStepChain Step<T>(string name, Func<dynamic[], T> function, out T returnValue, params dynamic[] args);
    IStepChain Step<T>(string name, Func<T> function, out T returnValue, params dynamic[] args);
```
* We only have one overloaded method Here *Step*.
* Step accept different parameters to allow test a flexible usage
* The minimum parameter is an Action/Function (with the logic of the step)
* In the case of a function, the return value will be provided in an out parameter.
* Step accepts a name for the specific step
* Optionally arguments could be sent if the Action/Function need them

2. Build the abstract class

 ```cs
public abstract class StepChain : IStepChain
{
    private int _stepN;

    public StepChain()
    {
        _stepN = 1;
    }

    public IStepChain Step(Action<dynamic[]> action, params dynamic[] args) => Step(action.ToString(), action, args);
    public IStepChain Step(Action action) => Step(action.ToString(), action);
    public IStepChain Step<T>(Func<T> function, out T returnValue, params dynamic[] args) => Step(function.ToString(), args => { return function(); }, out returnValue, args);
    public IStepChain Step<T>(Func<dynamic[], T> function, out T returnValue, params dynamic[] args) => Step(function.ToString(), args => { return function(args); }, out returnValue, args);

    public IStepChain Step(string name, Action<dynamic[]> action, params dynamic[] args) => Step(name, args => { action(args); return true; }, out _, args);
    public IStepChain Step(string name, Action action) => Step(name, args => { action(); return true; }, out _);
    public IStepChain Step<T>(string name, Func<T> function, out T returnValue, params dynamic[] args) => Step(name, args => { return function(); }, out returnValue);
    public virtual IStepChain Step<T>(string name, Func<dynamic[], T> function, out T returnValue, params dynamic[] args)
    {
       
        try
        {
            BeforeStep(name, function, args);
            returnValue = function(args);
            AfterStep(name, function, returnValue, args);
            return this;
        }
        catch(Exception e)
        {
            ErrorStep(name, function, e, args);
            throw e;
        }
        finally
        {
            _stepN++;
        }
    }
    public abstract void AfterStep<T>(string name, Func<dynamic[], T> function, T returnValue, params dynamic[] args);
    public abstract void BeforeStep<T>(string name, Func<dynamic[], T> function, params dynamic[] args);
    public abstract void ErrorStep<T>(string name, Func<dynamic[], T> function, Exception e, params dynamic[] args);
```
* All the overloaded methods are implemented in once fullfilling the extra parameters with defaults
* AfterStep abstract method is defined forcing any StepChain to implement it with the logic needed to execute after the step is executed
* BeforeStep abstract method is defined forcing any StepChain to implement it with the logic needed to execute before the Step execution
* ErrorStep abstract method is defined forcing any StepChain to implement it with the logic needed to execute if any error happens during the step execution
* Step logic is a simple try/catch structure that execute beforeStep, The logic step sent at parameters, AfterStep (in case of success) or ErrorStep (in case of error)
* The chain initialize the step number to 1. That means we will use a new stepchain per test


3. Create our StepChain (in this case with a execution summary)

 ```cs
public class StepsWithSummary : StepChain
{
    private Stopwatch _stopWatch;
    private string _summaryPath;

    public StepsWithSummary(string summaryPath)
    {
        _stopWatch = new Stopwatch();
        _summaryPath = summaryPath;
    }

    public override void BeforeStep<T>(string name, Func<dynamic[], T> function, params dynamic[] args)
    {
        _stopWatch.Restart();
    }
    public override void AfterStep<T>(string name, Func<dynamic[], T> function, T returnValue, params dynamic[] args)
    {
        this._stopWatch.Stop();
        TimeSpan ts = _stopWatch.Elapsed;
        File.AppendAllText(_summaryPath, $"{name}- Success:\n\t\tExecution time: {ts.TotalSeconds} seconds\n\t\tReturn value {returnValue} \n");
    }

    public override void ErrorStep<T>(string name, Func<dynamic[], T> function, Exception e, params dynamic[] args)
    {
        this._stopWatch.Stop();
        TimeSpan ts = _stopWatch.Elapsed;
        File.AppendAllText(_summaryPath, $"{name}- ERROR:\n\t\tExecution time: {ts.TotalSeconds} seconds\n\t\tException Value {e.Message} \n"); 
    }
}
```
* In the constructor, this class receives the path to store the summary with the execution time
* In the override BeforeStep. The internal StopWatch is restarted
* In the override AfterStep. The stopwatch is stopped, delta time is calculated and stored the relevant information in the file
* In the override ErrorSetp. The stopwatch is stopped, delta time is calculated and relevant information of the error is stored in the file

4. Apply StepChain in our tests

 ```cs
[TestClass]
public class StepChainPoc
{
    public IStepChain Chain;
    

    [TestInitialize]
    public void Setup()
    {
        Chain = new StepsWithSummary("./Summary.txt");
    }

    [TestMethod]
    public void TestMethod1()
    {
        Chain.Step("first step", () => Thread.Sleep(2000))
            .Step("Second step", () => Thread.Sleep(1200))
            .Step("Third step", (time) => Thread.Sleep(time[0]), 200)
            .Step("Forth step", () => true, out var returnvalue)
            .Step("Fifth step", () => throw new AccessViolationException("You can't"));
    }

}
```
* StepChain is initialized at the testInitialize, so, there is one new instance for test
* Test use the Chain to run its steps, by adding the name step and the Action/Function with the logic and parameters
* To make this example meaningful, the actions are mainly thread sleep and exception raising

5. Check the results
 ```
first step- Success:
		Execution time: 2.0026198 seconds
		Return value True 
Second step- Success:
		Execution time: 1.2098106 seconds
		Return value True 
Third step- Success:
		Execution time: 0.2619657 seconds
		Return value True 
Forth step- Success:
		Execution time: 0.0001191 seconds
		Return value True 
Fifth step- ERROR:
		Execution time: 0.0593111 seconds
		Exception Value You can't 
```

# Extras
This implementation could easily be extended for adapt to the project ubiquitus language. Check the flavours to see examples for Gherkin and AAA aproach.

