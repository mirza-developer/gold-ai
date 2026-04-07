using Microsoft.ML.Data;

namespace GoldAI.ML.Models;

/// <summary>
/// Binary-classification prediction output from one ML.NET model.
/// </summary>
public class BinaryPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    [ColumnName("Score")]
    public float Score { get; set; }
}
