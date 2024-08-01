using StepsForUnit.abstractions;
using System.Diagnostics;

namespace StepsForUnit;
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
