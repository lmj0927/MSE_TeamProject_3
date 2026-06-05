using System.Collections.Generic;

/// <summary>
/// <c>gameProgress</c>와 <see cref="StageSO.oneStarScore"/>로 스테이지 언락 여부를 판단합니다.
/// </summary>
public static class StageProgressGate
{
    public static StageSO GetStageDef(StageSO[] stages, int stageNumber)
    {
        if (stages == null || stageNumber < 1)
            return null;
        var idx = stageNumber - 1;
        return idx < stages.Length ? stages[idx] : null;
    }

    /// <summary>해당 스테이지를 1스타 이상으로 클리어했는지.</summary>
    public static bool IsStageCleared(int stageNumber, IReadOnlyDictionary<string, int> progress, StageSO[] stages)
    {
        if (stageNumber < 1)
            return false;
        if (progress == null || !progress.TryGetValue(stageNumber.ToString(), out var score))
            return false;

        var def = GetStageDef(stages, stageNumber);
        if (def == null)
            return score > 0;

        return score >= def.oneStarScore;
    }

    /// <summary>Stage 1은 항상 언락. Stage N은 Stage N-1 클리어(1스타+) 필요.</summary>
    public static bool IsStageUnlocked(int stageNumber, IReadOnlyDictionary<string, int> progress, StageSO[] stages)
    {
        if (stageNumber <= 1)
            return true;
        return IsStageCleared(stageNumber - 1, progress, stages);
    }

    public static string FormatStageLabel(int stageNumber, StageSO def)
    {
        if (def != null && !string.IsNullOrWhiteSpace(def.stageName))
            return def.stageName;
        return $"Level {stageNumber}";
    }
}
