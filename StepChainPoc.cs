using StepsForUnit.abstractions;

namespace StepsForUnit;

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