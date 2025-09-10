using Microsoft.ML;
using Microsoft.ML.AutoML;
using System.Diagnostics;

namespace TheGame.PlateTrainer;

public enum MulticlassRankerMetric
{
  Ndcg = 0,
  LogLoss = 1,
  TopK = 2,
}

public sealed class MulticlassRankerTrialRunner(MLContext mlContext,
  ModelEvaluationService modelEvalService,
  SweepablePipeline sweepablePipeline,
  IDataView trainSet,
  MulticlassRankerMetric metricToUse,
  int seed,
  int cvFolds,
  int k) : ITrialRunner
{
  private readonly static string[] _labelsPlaceholder = Enumerable.Range(0, 51)
    .Select(i => $"{i}")
    .ToArray();

  public Task<TrialResult> RunAsync(TrialSettings settings, CancellationToken ct)
  {
    var sw = Stopwatch.StartNew();
    var parameter = settings.Parameter["_pipeline_"];

    var pipeline = sweepablePipeline.BuildFromOption(mlContext, parameter);

    var folds = mlContext.Data.CrossValidationSplit(trainSet,
      numberOfFolds: cvFolds,
      seed: seed);

    var foldMetrics = folds
      .Select(fold =>
      {
        var model = pipeline.Fit(fold.TrainSet);
        var scored = model.Transform(fold.TestSet);

        return modelEvalService.CalculateMetricsForSet(scored, _labelsPlaceholder, k);
      })
      .ToList();

    var model = pipeline.Fit(trainSet);

    var cvMetrics = SetMetrics.FromAverage(foldMetrics);

    sw.Stop();

    var trialResult = new TrialResult()
    {
      DurationInMilliseconds = sw.ElapsedMilliseconds,
      Loss = cvMetrics.LogLoss,
      Metric = metricToUse switch
      {
        MulticlassRankerMetric.Ndcg => cvMetrics.Ndcg,
        MulticlassRankerMetric.LogLoss => cvMetrics.LogLoss,
        MulticlassRankerMetric.TopK => cvMetrics.TopKAccuracy,
        _ => throw new InvalidOperationException($"Unknown metric to use")
      },
      Model = model,
      TrialSettings = settings,
      PeakCpu = null,
      PeakMemoryInMegaByte = null,
    };

    return Task.FromResult(trialResult);
  }

  public void Dispose()
  {
    return;
  }
}
